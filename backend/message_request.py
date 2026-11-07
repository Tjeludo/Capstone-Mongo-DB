from .connect import connect_to_mongodb
from pymongo import errors, ASCENDING, DESCENDING
from datetime import datetime

# Establish a connection to the new MongoDB collection.
message_requests_table = connect_to_mongodb("message_requests")


class MessageRequest:
    def __init__(self, title, body, requester_id):
        self.request_id = self.generate_request_id()
        self.title = title.title()
        self.body = body
        self.requester_id = requester_id  # This will be the manager's staff_id
        self.status = "Pending"  # Default status for any new request
        self.reviewer_id = None  # No admin has reviewed it yet
        self.created_at = datetime.now().strftime("%d-%m-%Y %H:%M")
        self.updated_at = self.created_at

    def save(self):
        """Saves a new message request to the database."""
        try:
            message_requests_table.insert_one(self.to_dict())
            return {"success": True, "message": "Request submitted successfully."}
        except errors.OperationFailure as e:
            return {"success": False, "message": f"Database error: {e}"}

    def to_dict(self):
        """Converts the MessageRequest object to a dictionary for MongoDB."""
        return {
            "request_id": self.request_id,
            "title": self.title,
            "body": self.body,
            "requester_id": self.requester_id,
            "status": self.status,
            "reviewer_id": self.reviewer_id,
            "created_at": self.created_at,
            "updated_at": self.updated_at,
        }

    def generate_request_id(self):
        """
        Generates a new, sequential request ID (e.g., REQ0001, REQ0002).
        """
        try:
            # Find the document with the highest request_id.
            doc = message_requests_table.find_one(sort=[("request_id", -1)])
            if doc and "request_id" in doc and doc["request_id"].startswith("REQ"):
                last_id = doc["request_id"]
                num = int(last_id[3:])  # Extract the number part (e.g., from 'REQ0009' to 9).
                new_id = num + 1
            else:
                # This is the first request in the collection.
                new_id = 1

            # Format the new code as REQ#### (e.g., REQ0001, REQ0012).
            request_id = f"REQ{new_id:04d}"
            return request_id
        except (errors.OperationFailure, ValueError) as err:
            # TODO: Log the actual error 'err'.
            return None

    @staticmethod
    def find_by_requester(staff_id):
        """Finds all requests made by a specific manager, sorted with pending first."""
        try:
            pipeline = [
                {"$match": {"requester_id": staff_id}},
                {
                    "$addFields": {
                        "sort_order": {
                            "$switch": {
                                "branches": [
                                    {"case": {"$eq": ["$status", "Pending"]}, "then": 1},
                                    {"case": {"$eq": ["$status", "Approved"]}, "then": 2},
                                    {"case": {"$eq": ["$status", "Declined"]}, "then": 3},
                                ],
                                "default": 4
                            }
                        }
                    }
                },
                {"$sort": {"sort_order": ASCENDING, "created_at": DESCENDING}},
                {"$project": {"_id": 0, "sort_order": 0}}
            ]
            requests = list(message_requests_table.aggregate(pipeline))
            return {"success": True, "data": requests}
        except errors.OperationFailure as e:
            return {"success": False, "message": f"Database error: {e}"}

    @staticmethod
    def find_all():
        """Finds all requests, sorted with pending first."""
        try:
            pipeline = [
                {
                    "$addFields": {
                        "sort_order": {
                            "$switch": {
                                "branches": [
                                    {"case": {"$eq": ["$status", "Pending"]}, "then": 1},
                                    {"case": {"$eq": ["$status", "Approved"]}, "then": 2},
                                    {"case": {"$eq": ["$status", "Declined"]}, "then": 3},
                                ],
                                "default": 4
                            }
                        }
                    }
                },
                {"$sort": {"sort_order": ASCENDING, "created_at": DESCENDING}},
                {"$project": {"_id": 0, "sort_order": 0}}
            ]
            requests = list(message_requests_table.aggregate(pipeline))
            return {"success": True, "data": requests}
        except errors.OperationFailure as e:
            return {"success": False, "message": f"Database error: {e}"}

    @staticmethod
    def update_status(request_id, new_status, admin_id):
        """Updates the status of a request (approve/decline)."""
        new_status = new_status.title()
        if new_status not in ["Approved", "Declined"]:
            return {"success": False, "message": "Invalid status."}

        try:
            result = message_requests_table.update_one(
                {"request_id": request_id},
                {
                    "$set": {
                        "status": new_status,
                        "reviewer_id": admin_id,
                        "updated_at": datetime.now().strftime("%d-%m-%Y %H:%M"),
                    }
                },
            )
            if result.modified_count == 1:
                return {"success": True, "message": f"Request {new_status}."}
            else:
                return {"success": False, "message": "Request not found."}
        except errors.OperationFailure as e:
            return {"success": False, "message": f"Database error: {e}"}