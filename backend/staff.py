import secrets
import string
from datetime import datetime
from .connect import connect_to_mongodb
from pymongo import errors
import pandas as pd
import io
from email.mime.text import MIMEText
import os
from smtplib import SMTP
from dotenv import load_dotenv, find_dotenv
from .user import User
from fastapi import UploadFile

# Connect to the MongoDB collections I'll need for staff operations.
staff_table = connect_to_mongodb("staff")
roles_table = connect_to_mongodb("roles")
users_table = connect_to_mongodb("users")


class Staff:
    def __init__(self, branch_code, role, fname, sname, email, dob, gender, cell, addr, status, start_date):
        """
        Initializes a new staff member instance.
        This is where I clean and format all the incoming data before it's used.
        """
        self.branch_code = branch_code
        self.role = role
        # Format names and address to Title Case, and email to lowercase.
        self.fname = fname.strip().title()
        self.sname = sname.strip().title()
        self.date_of_birth = dob
        self.gender = gender
        self.email = email.strip().lower()
        self.staff_email = self.generate_staff_email()
        self.phone_num = cell.strip()
        self.address = addr.strip().title()
        self.status = status
        # Generate the unique ID and set the start date upon creation.
        self.staff_id = self.generate_staff_id()
        self.start_date = start_date

    def add_staff(self):
        """
        Adds the new staff member to the database.
        Also creates their user account for logging in and sends their credentials via email.
        """
        if self.staff_id is None:
            return {"success": False, "message": "Failed to generate a staff ID.", "data": ""}

        doc = {
            "staff_id": self.staff_id,
            "branch_code": self.branch_code,
            "role": self.role,
            "firstname": self.fname,
            "lastname": self.sname,
            "date_of_birth": self.date_of_birth,
            "gender": self.gender,
            "email": self.email,
            "staff_email": self.staff_email,
            "phone_number": self.phone_num,
            "address": self.address,
            "status": self.status,
            "start_date": self.start_date,
        }

        # Make sure this staff member doesn't already exist.
        if Staff.staff_exists(self.staff_id):
            return {"success": False, "message": "Staff member already exists.", "data": ""}
        else:
            try:
                staff_pass = self.generate_passwd()
                # Send the email before creating the database entries. If email fails, I might reconsider this.
                self.send_credentials(staff_pass)
                staff_table.insert_one(doc)
                # Create the corresponding user login.
                User(self.staff_email, self.staff_id, staff_pass).add_user()
                return {"success": True, "message": "Staff created successfully.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: Log this error properly.
                return {"success": False, "message": "Staff creation failed due to a database error.", "data": ""}

    def generate_staff_id(self):
        """
        Generates a unique staff ID based on role and a sequence.
        Format: STF-{RoleCode}{Sequence}{Year} (e.g., STF-010125)
        """
        # First, check if a staff member with this exact name/email combo already exists to prevent duplicates.
        existing_staff = staff_table.find_one({
            "firstname": self.fname,
            "lastname": self.sname,
            "email": self.email
        })
        if existing_staff:
            return existing_staff['staff_id']

        try:
            # Find the numeric code for the role (e.g., 'MAN' -> '01').
            role_doc = roles_table.find_one({"role_abbrev": self.role}, {"_id": 0})
            if not role_doc:
                return None  # Can't generate an ID without a valid role.

            role_code = role_doc["role_code"]
            prefix = f"STF-{int(role_code):02}"

            # Find the last staff member with this role prefix to determine the next number in the sequence.
            last_staff = staff_table.find_one({"staff_id": {"$regex": f"^{prefix}"}}, sort=[("staff_id", -1)])

            new_code = 1
            if last_staff:
                last_id_num = int(last_staff["staff_id"][6:8])
                new_code = last_id_num + 1

            year = str(datetime.now().year)[2:]
            return f"{prefix}{new_code:02}{year}"
        except (errors.OperationFailure, ValueError) as err:
            # TODO: Log this error.
            return None

    def generate_staff_email(self):
        """Creates a standard company email from the staff member's name."""
        fname_part = self.fname.lower().strip().replace(" ", ".")
        sname_part = self.sname.lower().strip().replace(" ", ".")
        return f"{fname_part}.{sname_part}@baby-bargains.co.za"

    def generate_passwd(self):
        """Generates a secure, random 16-character password."""
        alphabet = string.ascii_letters + string.digits + string.punctuation
        return ''.join(secrets.choice(alphabet) for _ in range(16))

    def send_credentials(self, password):
        """Emails the new staff member their login credentials."""
        load_dotenv(find_dotenv())
        # My app's email credentials, stored securely in the .env file.
        email_user = os.environ.get("email")
        email_passwd = os.environ.get("email_app_passwd")
        smtp_server = "smtp.gmail.com"

        html_message = f"""
            <html><body>
                <p>Dear {self.fname},</p>
                <p>Welcome to Baby Bargains. Your staff account has been created. Your login details are:</p>
                <p>
                Staff ID: <b>{self.staff_id}</b><br>
                Email: <b>{self.staff_email}</b><br>
                Password: <b>{password}</b>
                </p>
                <p>Please keep this information secure.</p>
                <p>Best regards,<br>Baby Bargains</p>
            </body></html>"""
        msg = MIMEText(html_message, "html")
        msg["Subject"] = "NO REPLY: Staff Account Login Details"
        msg["From"] = email_user
        msg["To"] = self.email

        try:
            with SMTP(smtp_server, 587) as client:
                client.starttls()
                client.login(user=email_user, password=email_passwd)
                client.sendmail(email_user, self.email, msg.as_string())
        except Exception as err:
            # It's important to know if emails are failing to send.
            print(f"ERROR: Email failed to send to {self.email}. Reason: {err}")

    @staticmethod
    def staff_exists(staff_id):
        """A simple check to see if a staff member with this ID is already in the database."""
        try:
            return staff_table.count_documents({"staff_id": staff_id}) > 0
        except errors.OperationFailure:
            return False

    @staticmethod
    def update_staff(staff_id, fname, sname, gender, phone, addr, status):
        """Updates the details of an existing staff member."""
        if Staff.staff_exists(staff_id):
            try:
                staff_table.update_one(
                    {"staff_id": staff_id},
                    {"$set": {
                        # Apply the same formatting on update as on creation.
                        "firstname": fname.strip().title(),
                        "lastname": sname.strip().title(),
                        "gender": gender,
                        "phone_number": phone.strip(),
                        "address": addr.strip().title(),
                        "status": status
                    }}
                )
                return {"success": True, "message": "Staff updated successfully.", "data": ""}
            except errors.OperationFailure:
                return {"success": False, "message": "Staff update failed.", "data": ""}
        else:
            return {"success": False, "message": "Staff member does not exist.", "data": ""}

    @staticmethod
    def view_staff():
        """Retrieves a list of all staff members from the database."""
        try:
            data = list(staff_table.find({}, {"_id": 0}))
            return {"success": True, "message": "", "data": data} if data else {"success": False,
                                                                                "message": "No staff found.",
                                                                                "data": ""}
        except errors.OperationFailure:
            return {"success": False, "message": "Failed to retrieve staff.", "data": ""}

    # Search staff
    @staticmethod
    def search_staff(staff_id):
        if Staff.staff_exists(staff_id):
            try:
                doc = staff_table.find_one({"staff_id": staff_id}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Staff not found.", "data": ""}
            except errors.OperationFailure as err:
                # log error
                return {"success": False, "message": "Failed to retrieve staff.", "data": ""}
        else:
            return {"success": False, "message": "Staff does not exist.", "data": ""}

    @staticmethod
    def delete_staff(staff_id):
        """Deletes a staff member from the staff collection and also removes their user login."""
        if Staff.staff_exists(staff_id):
            try:
                staff_table.delete_one({"staff_id": staff_id})
                users_table.delete_one({"staff_id": staff_id})
                return {"success": True, "message": "Staff deleted successfully.", "data": ""}
            except errors.OperationFailure:
                return {"success": False, "message": "Staff deletion failed.", "data": ""}
        else:
            return {"success": False, "message": "Staff member does not exist.", "data": ""}

    @staticmethod
    def transfer_staff(staff_id, branch_code):
        """Updates a staff member's branch code."""
        if Staff.staff_exists(staff_id):
            try:
                staff_table.update_one({"staff_id": staff_id}, {"$set": {"branch_code": branch_code}})
                return {"success": True, "message": "Staff transferred successfully.", "data": ""}
            except errors.OperationFailure:
                return {"success": False, "message": "Staff transfer failed.", "data": ""}
        else:
            return {"success": False, "message": "Staff member does not exist.", "data": ""}

    @staticmethod
    def promote_staff(staff_id, new_role):
        """Updates a staff member's role."""
        if Staff.staff_exists(staff_id):
            try:
                staff_table.update_one({"staff_id": staff_id}, {"$set": {"role": new_role}})
                return {"success": True, "message": "Staff promoted successfully.", "data": ""}
            except errors.OperationFailure:
                return {"success": False, "message": "Staff promotion failed.", "data": ""}
        else:
            return {"success": False, "message": "Staff member does not exist.", "data": ""}

    # Update staff status
    @staticmethod
    def update_staff_status(staff_id, status):
        if Staff.staff_exists(staff_id):
            try:
                staff_table.update_one(
                    {"staff_id": staff_id},
                    {"$set": {"status": status}}
                )
                return {"success": True, "message": "Status updated successfully.", "data": ""}
            except errors.OperationFailure as err:
                # log err
                return {"success": False, "message": "Status update failed.", "data": ""}
        else:
            return {"success": False, "message": "Staff does not exist.", "data": ""}

    # Staff info
    @staticmethod
    def staff_info(staff_id):
        if Staff.staff_exists(staff_id):
            doc = staff_table.find_one({"staff_id": staff_id}, {"_id": 0})
            return doc
        return None

    # Download csv
    @staticmethod
    def download_staff_csv():
        try:
            data = list(staff_table.find({}, {"_id": 0}))
            if data:
                df = pd.DataFrame(data)
                csv_buffer = io.StringIO()
                df.to_csv(csv_buffer, index=False)
                csv_content = csv_buffer.getvalue()
                return csv_content
            else:
                return ""
        except errors.OperationFailure as err:
            # log err
            pass

    # Upload csv
    @staticmethod
    async def upload_staff_csv(file: UploadFile):
        try:
            # Check if table is empty - only allow upload on empty table
            existing_count = staff_table.count_documents({})
            if existing_count > 0:
                return {
                    "success": False,
                    "message": "Upload failed. Table already contains data.",
                    "data": ""
                }

            # Read file content
            contents = await file.read()

            # Create DataFrame from CSV
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # Validate required columns
            required_columns = ['branch_code', 'role', 'firstname', 'lastname',
                                'email', 'date_of_birth', 'gender', 'phone_number',
                                'address', 'status']

            if not all(col in df.columns for col in required_columns):
                return {
                    "success": False,
                    "message": f"CSV must contain exactly these columns:\n{', '.join(required_columns)}",
                    "data": ""
                }

            # Check for unexpected columns (optional)
            allowed_columns = required_columns
            extra_columns = [col for col in df.columns if col not in allowed_columns]
            if extra_columns:
                return {
                    "success": False,
                    "message": f"CSV contains unexpected columns.\nOnly {', '.join(allowed_columns)} are allowed.",
                    "data": ""
                }

            # Process each row
            has_errors = False

            for index, row in df.iterrows():
                try:
                    # Extract and validate data
                    branch_code = str(row['branch_code']).strip()
                    role = str(row['role']).strip()
                    firstname = str(row['firstname']).strip()
                    lastname = str(row['lastname']).strip()
                    email = str(row['email']).strip()
                    date_of_birth = str(row['date_of_birth']).strip()
                    gender = str(row['gender']).strip()
                    phone_number = str(row['phone_number']).strip()
                    address = str(row['address']).strip()
                    status = str(row['status']).strip()

                    # Skip empty rows
                    if (not firstname or firstname.lower() == 'nan' or pd.isna(row['firstname']) or
                            not lastname or lastname.lower() == 'nan' or pd.isna(row['lastname']) or
                            not email or email.lower() == 'nan' or pd.isna(row['email'])):
                        continue

                    # Create new staff (this will auto-generate staff_id)
                    staff = Staff(branch_code, role, firstname, lastname, email,
                                  date_of_birth, gender, phone_number, address, status,start_date=datetime.now())
                    result = staff.add_staff()

                    if not result["success"]:
                        has_errors = True
                        break

                except Exception as row_error:
                    has_errors = True
                    break

            # Return simple success or failure
            if has_errors:
                return {
                    "success": False,
                    "message": "Upload unsuccessful & incomplete.",
                    "data": ""
                }
            else:
                return {
                    "success": True,
                    "message": "Upload successful & complete.",
                    "data": ""
                }

        except Exception as err:
            return {
                "success": False,
                "message": f"Upload failed.",
                "data": ""
            }


if __name__ == "__main__":
    try:
        staff_table.create_index("staff_id", unique=True)
    except errors.OperationFailure as e:
        pass