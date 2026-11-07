from datetime import datetime, timedelta
import secrets
import string
import bcrypt
from .connect import connect_to_mongodb
from tabulate import tabulate
from pymongo import errors
from dotenv import load_dotenv, find_dotenv
from email.mime.text import MIMEText
import os
from smtplib import SMTP
from twilio.rest import Client

load_dotenv(find_dotenv())
users_table = connect_to_mongodb("users")
roles_table = connect_to_mongodb("roles")
staff_table = connect_to_mongodb("staff")


class User:
    def __init__(self, staff_email, staff_id, password):
        self.staff_email = staff_email
        self.staff_id = staff_id
        self.password = password
        self.status = "active"
        self.created_at = datetime.now().strftime("%d-%m-%Y %H:%M")

    # ! Add
    def add_user(self):
        hashed_password = User.set_password(self.password)
        if hashed_password is None:
            return {"success": False, "message": "Invalid password format"}

        user_data = {
            "staff_email": self.staff_email,
            "staff_id": self.staff_id,
            "password": hashed_password,
            "status": self.status,
            "created_at": self.created_at
        }
        try:
            users_table.insert_one(user_data)
            print(f"[SUCCESS] User '{self.staff_id}' created.")
            return {"success": True, "message": f"User '{self.staff_id}' created."}
        except errors.DuplicateKeyError:
            print(f"[ERROR] User already exists.")
            return {"success": False, "message": "User already exists."}
        except errors.OperationFailure as e:
            print(f"[ERROR] User creation failed: {e}")
            return {"success": False, "message": f"User creation failed: {e}"}

    # ! Delete
    @staticmethod
    def delete_user(staff_id):
        # check if id exists
        try:
            result = users_table.delete_one({"staff_id": staff_id})
            if result.deleted_count:
                print(f"[SUCCESS] User '{staff_id}' deleted.")
                return {"success": True, "message": f"User '{staff_id}' deleted."}
            print(f"[INFO] User '{staff_id}' not found.")
            return {"success": False, "message": f"User '{staff_id}' not found."}
        except Exception as e:
            print(f"[ERROR] User deletion failed: {e}")
            return {"success": False, "message": f"User deletion failed: {e}"}

    # ! Search
    @staticmethod
    def search_user(staff_id):
        # check if id exists
        try:
            user = users_table.find_one({"staff_id": staff_id},
                                        {"_id": 0, "password": 0, "otp_code": 0, "otp_expiry": 0})
            if user:
                headers = user.keys()
                rows = [user.values()]
                print(tabulate(rows, headers=headers, tablefmt="simple"))
                return {"success": True, "data": user}
            else:
                print(f"[INFO] User '{staff_id}' not found.")
                return {"success": False, "message": f"User '{staff_id}' not found."}
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve user: {e}")
            return {"success": False, "message": f"Failed to retrieve user: {e}"}

    # ! Status
    @staticmethod
    def update_user_status(staff_id, status):
        try:
            result = users_table.update_one(
                {"staff_id": staff_id},
                {"$set": {"status": status}}
            )
            if result.modified_count:
                return {"success": True, "message": f"User status updated successfully."}
            return {"success": False, "message": f"User '{staff_id}' not found or status unchanged."}
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to update user status: {e}")
            return {"success": False, "message": f"Failed to update user status: {e}"}

    # ! Lookup
    @staticmethod
    def user_exists(staff_id):
        try:
            user = users_table.find_one({"staff_id": staff_id})
            if user:
                return {"success": True, "message": f"True"}
            return {"success": False, "message": f"False"}
        except errors.OperationFailure as e:
            print(f"[INFO] User '{staff_id}' not found: {e}")
            return {"success": False, "message": f"User lookup failed: {e}"}

    # ! View
    @staticmethod
    def view_users():
        try:
            # Exclude sensitive fields from the view
            cursor = users_table.find({}, {"_id": 0, "password": 0, "otp_code": 0, "otp_expiry": 0})
            data = list(cursor)
            if data:
                return {"success": True, "data": data}
            else:
                print("[INFO] No users found.")
                return {"success": False, "message": "No users found."}
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve users: {e}")
            return {"success": False, "message": f"Failed to retrieve users: {e}"}

    # Login: (Part 1: Password Check & 2FA Code Generation)
    @staticmethod
    def log_in(staff_id, password):
        staff_id = staff_id.strip().upper()
        try:
            user = users_table.find_one({"staff_id": staff_id})
            if user:
                hashed_password = user.get("password")
                if hashed_password and bcrypt.checkpw(password.encode("utf-8"), hashed_password.encode("utf-8")):
                    # Password is correct, now generate and send 2FA code
                    otp = ''.join(secrets.choice(string.digits) for _ in range(6))
                    # Code is valid for 10 minutes
                    otp_expiry = datetime.now() + timedelta(minutes=10)

                    users_table.update_one(
                        {"staff_id": staff_id},
                        {"$set": {"otp_code": otp, "otp_expiry": otp_expiry}})
                    # to use staff email when domain is active
                    user = staff_table.find_one({"staff_id": staff_id}, {"_id": 0})
                    # Send the code via email
                    User.send_2fa_email(user["email"],staff_id, otp)
                    # Send the code via sms
                    #User.send_2fa_sms(staff_id, otp)

                    return {"success": True, "message": "2FA required"}
                    # return {"success": True, "message": "Login successful"}

            return {"success": False, "message": "Login failed. Incorrect username/password."}
        except errors.OperationFailure as e:
            # log err
            return {"success": False, "message": f"Login failed: {e}"}

    # MFA: Verify 2FA (Part 2: Code Check & Session Creation)
    @staticmethod
    def verify_2fa(staff_id, otp_code):
        staff_id = staff_id.strip().upper()
        try:
            user = users_table.find_one({"staff_id": staff_id})
            if user and user.get("otp_code") == otp_code:
                # OTP is correct and check if not expired
                if datetime.now() < user.get("otp_expiry"):
                    # Clear the OTP after use for security
                    users_table.update_one({"staff_id": staff_id}, {"$unset": {"otp_code": "", "otp_expiry": ""}})
                    # get user info
                    user = staff_table.find_one({"staff_id": staff_id}, {"_id": 0})
                    # get user role
                    user_role = user["role"]
                    # get role permissions
                    role = roles_table.find_one({"role_abbrev": user_role}, {"_id": 0})
                    permissions = role["permissions"]

                    return {
                        "success": True,
                        "message": "Login successful",
                        "data": {
                            "staff_id": user["staff_id"],
                            "staff_name": user.get("firstname", "N/A"),
                            "staff_surname": user.get("lastname", "N/A"),
                            "staff_phone":user.get("phone_number", "N/A"),
                            "staff_email": user.get("staff_email", "N/A"),
                            "role": role["role_name"],
                            "permissions": permissions
                        }
                    }
                else:
                    return {"success": False, "message": "OTP code has expired."}
            return {"success": False, "message": "Invalid OTP code."}
        except errors.OperationFailure as e:
            print(f"[ERROR] 2FA verification failed: {e}")
            return {"success": False, "message": f"2FA verification failed: {e}"}

    @staticmethod
    def send_2fa_sms(staff_id,otp):
        staff_id = staff_id.strip().upper()
        user = staff_table.find_one({"staff_id": staff_id}, {"_id": 0})
        name = user["firstname"]
        phone = user["phone_number"]
        account_sid = os.environ.get('TwilioSID')
        auth_token = os.environ.get('TwilioAuthToken')
        client = Client(account_sid, auth_token)
        message = client.messages.create(
            to=phone,
        from_ = "+12542673599",
        body = f"Hi {name},\nYour Baby Bargains login verification code is: {otp}")
        print(message.sid)

    @staticmethod
    def send_2fa_email(user_email, staff_id,code):
        staff_id = staff_id.strip().upper()
        load_dotenv(find_dotenv())
        email = os.environ.get("email")
        email_passwd = os.environ.get("email_app_passwd")
        smtp_server = "smtp.gmail.com"
        name = staff_table.find_one({"staff_id": staff_id}, {"_id": 0})["firstname"]
        sender = email
        recipient = user_email
        subject = "NO REPLY: 2FA Code"
        html_message = f"""
                            <html>
                            <body>
                                <p>Dear {name},</p>

                                <p>Here is your OTP:</p>

                                <p><b>{code}</b><br></p>

                                <p>Please keep this information secure and do not share it with anyone.</p>
                                <p>
                                If you have any questions or experience issues accessing your account, please contact our support team at 
                                <b>support@baby-bargains.co.za</b>.
                                </p>
                                <p>Best regards,<p>
                                <p>Baby Bargains</p>
                            </body>
                            </html> """

        msg = MIMEText(html_message, "html")
        msg["Subject"] = subject
        msg["From"] = sender
        msg["To"] = recipient

        try:
            with SMTP(smtp_server) as client:
                client.starttls()
                client.login(user=email, password=email_passwd)
                client.sendmail(sender, recipient, msg.as_string())
                return {"success": True, "message": f"Email sent to '{email}'."}
        except Exception as err:
            # log err
            return {"success": False, "message": f"Couldn't send email to '{email}'."}

    # Change pass
    @staticmethod
    def change_password(staff_id, old_password, new_password):
        try:
            user = users_table.find_one({"staff_id": staff_id})
            if user:
                hashed_password = user.get("password")
                if hashed_password and bcrypt.checkpw(old_password.encode("utf-8"), hashed_password.encode("utf-8")):
                    new_hashed = User.set_password(new_password)
                    if new_hashed:
                        users_table.update_one({"staff_id": staff_id}, {"$set": {"password": new_hashed}})
                        print("Password updated successfully.")
                        return {"success": True, "message": f"Password updated successfully."}
            print("Password change failed.")
            return {"success": False, "message": f"Password change failed."}
        except errors.OperationFailure as e:
            print(f"[ERROR] Password change failed: {e}")
            return {"success": False, "message": f"Password change failed: {e}"}

    # ! Reset pass
    @staticmethod
    def reset_password(staff_email):
        try:
            user = users_table.find_one({"staff_email": staff_email})
            if user:
                # Generate new password and update
                import secrets
                new_password = ''.join(
                    secrets.choice(string.ascii_letters + string.digits + string.punctuation) for _ in range(16))
                hashed_password = User.set_password(new_password)
                if hashed_password:
                    users_table.update_one(
                        {"staff_email": staff_email},
                        {"$set": {"password": hashed_password}})
                    return {"success": True, "message": f"Password reset successfully.", "new_password": new_password}
            return {"success": False, "message": "User not found."}
        except errors.OperationFailure as e:
            print(f"[ERROR] Password reset failed: {e}")
            return {"success": False, "message": f"Password reset failed: {e}"}

    # ! Set pass
    @staticmethod
    def set_password(password):
        length_ok = len(password) >= 16
        has_lower = any(c.islower() for c in password)
        has_upper = any(c.isupper() for c in password)
        has_digit = any(c.isdigit() for c in password)
        has_punct = any(c in string.punctuation for c in password)

        if length_ok and has_lower and has_upper and has_digit and has_punct:
            password_bytes = password.encode('utf-8')
            hashed_bytes = bcrypt.hashpw(password_bytes, bcrypt.gensalt())
            return hashed_bytes.decode('utf-8')  # Return the actual hashed password as string

        print("Password invalid! Must be 16+ chars with lowercase, uppercase, number, and punctuation.")
        return None  # Return None if password is invalid


if __name__ == "__main__":
    pass