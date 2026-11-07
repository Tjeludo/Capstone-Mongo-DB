from datetime import datetime
import secrets
import random
import string
import bcrypt
from connect import connect_to_mongodb
from tabulate import tabulate
from pymongo import errors
from dotenv import load_dotenv, find_dotenv

load_dotenv(find_dotenv())
guests_table = connect_to_mongodb("guests")

class Guest:
    def __init__(self, email, expiry_time):
        self.email = email
        self.guest_id = self.generate_guest_id()
        self.password = self.generate_password()
        self.status = "active"
        self.created_at = datetime.now().strftime("%d-%m-%Y %H:%M")
        self.expire_at = expiry_time

    # Add
    def add_guest(self):
        guest_data = {
            "guest_id": self.guest_id,
            "password": self.password,
            "status": self.status,
            "created_at": self.created_at,
            "expire_at": self.expire_at
        }
        try:
            guests_table.insert_one(guest_data)
            print(f"[SUCCESS] Guest '{self.guest_id}' created.")
            return {"success": True, "message": f"Guest created."}
        except errors.DuplicateKeyError:
            print(f"[ERROR] Guest already exists.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Guest creation failed: {e}")

    def generate_guest_id(self):
        # Generate format: GST-######
        try:
            doc = guests_table.find_one(sort=[("guest_id", -1)])
            if doc and "guest_id" in doc:
                last_id = doc["guest_id"]
                num = int(last_id[4:])  # Extract numeric part after GST-
                new_id = num + 1
            else:
                new_id = 1
            return f"GST-{new_id:06d}"
        except errors.OperationFailure:
            return None

    def generate_password(self):
        letters = [secrets.choice(string.ascii_letters) for _ in range(8)]
        digits = [secrets.choice(string.digits) for _ in range(4)]
        punctuation = [secrets.choice(string.punctuation) for _ in range(4)]
        combined_passwd = letters + digits + punctuation
        random.shuffle(combined_passwd)
        passwd = ''.join(combined_passwd)  # to email plain text
        password_bytes = passwd.encode('utf-8')
        hashed_bytes = bcrypt.hashpw(password_bytes, bcrypt.gensalt())
        passwd = hashed_bytes.decode('utf-8')
        return passwd

    # Delete
    @staticmethod
    def delete_guest(guest_id):
        # check if id exists
        try:
            result = guests_table.delete_one({"guest_id": guest_id})
            if result.deleted_count:
                print(f"[SUCCESS] Guest '{guest_id}' deleted.")
                return {"success": True, "message": f"Guest '{guest_id}' deleted."}
            print(f"[INFO] Guest '{guest_id}' not found.")
        except Exception as e:
            print(f"[ERROR] Guest deletion failed: {e}")

    # Search
    @staticmethod
    def search_guest(guest_id):
        # check if id exists
        try:
            guest = guests_table.find_one({"guest_id": guest_id}, {"_id": 0})
            if guest:
                headers = guest.keys()
                rows = [guest.values()]
                print(tabulate(rows, headers=headers, tablefmt="simple"))
            else:
                print(f"[INFO] Guest '{guest_id}' not found.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve guest: {e}")

    # Status
    @staticmethod
    def update_guest_status(guest_id):
        pass

    # View
    @staticmethod
    def view_guests():
        try:
            cursor = guests_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                headers = data[0].keys()
                rows = [guest.values() for guest in data]
                print(tabulate(rows, headers=headers, tablefmt="simple"))
                return {"success": True, "data": data}
            else:
                print("[INFO] No guests found.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve guests: {e}")

    # Login
    @staticmethod
    def guest_login(guest_id, password):
        try:
            guest = guests_table.find_one({"guest_id": guest_id})
            if guest:
                hashed_password = guest.get("password")
                if hashed_password and bcrypt.checkpw(password.encode("utf-8"), hashed_password.encode("utf-8")):
                    print("Login successful\n")
                    return {"success": True, "message": "Login successful"}
            print("Login failed. Incorrect guest_id/password.")
            return {"success": False, "message": "Login failed. Incorrect guest_id/password."}
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve credentials: {e}")


if __name__ == "__main__":
    pass