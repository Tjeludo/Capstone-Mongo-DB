from datetime import datetime, date, timedelta
import random
from pymongo import errors
from .connect import connect_to_mongodb
import pandas as pd
import io

orders_table = connect_to_mongodb("orders")


class Order:
    # These statuses define the lifecycle of an order and their logical progression.
    STATUS_ORDER = {
        "Pending": 0,
        "Confirmed": 1,
        "In-Progress": 2,
        "In-Transit": 3,
        "Partially Fulfilled": 4,
        "Fully Fulfilled": 5,
    }

    def __init__(self, supplier_id, staff_id):
        self.supplier_id = supplier_id
        self.order_id = self.generate_order_id()
        self.products = []
        self.total_cost = 0
        self.status = "Pending"
        self.ordered_by = staff_id
        self.order_date = datetime.now().strftime("%d-%m-%Y %H:%M")
        self.expect_delivery = self.generate_delivery_date().strftime("%d-%m-%Y")

    def generate_order_id(self):
        """
        Generates a new, unique order ID (e.g., ODR001, ODR002).
        It finds the last ID in the database and increments it.
        """
        try:
            # Find the document with the highest order_id to determine the next one.
            doc = orders_table.find_one(sort=[("order_id", -1)])
            if doc and "order_id" in doc:
                last_id = doc["order_id"]
                num = int(last_id[3:])  # 'ODR' is 3 chars.
                # Just a safety check to prevent overflow.
                if num >= 999:
                    raise ValueError("Maximum order_id reached (ODR999)")
                new_id = num + 1
            else:
                # This is the very first order.
                new_id = 1
            # Format to ODR001, ODR002, etc.
            order_id = f"ODR{new_id:03d}"
            return order_id
        except errors.OperationFailure as err:
            # log err
            return None
        except ValueError as err:
            # log err
            return None

    def generate_delivery_date(self):
        """
        Calculates an estimated delivery date 3 to 5 days from now,
        skipping Sundays.
        """
        start_date = date.today()
        days_to_add = random.randint(3, 5)
        estimated_date = start_date + timedelta(days=days_to_add)

        # In Python, Monday is 0 and Sunday is 6.
        if estimated_date.weekday() == 6:
            # If it's a Sunday, push it to Monday.
            final_date = estimated_date + timedelta(days=1)
        else:
            final_date = estimated_date
        return final_date

    def confirm_order(self):
        """
        Saves the new order to the database if it has products and a valid ID.
        """
        # Can't save an order if it failed to get an ID.
        if not self.order_id:
            return {"success": False, "message": "Failed to generate Order ID. Cannot confirm order.", "data": ""}

        if self.products:
            doc = {
                "order_id": self.order_id,
                "supplier_id": self.supplier_id,
                "products": self.products,
                "total_cost(R)": round(float(self.total_cost), 2),
                "status": self.status,
                "order_date": self.order_date,
                "ordered_by": self.ordered_by,
                "expect_delivery": self.expect_delivery
            }

            if Order.order_exists(self.order_id):
                return {"success": False, "message": "Order already exists.", "data": ""}
            else:
                try:
                    orders_table.insert_one(doc)
                    return {"success": True, "message": f"Order '{self.order_id}' confirmed successfully.", "data": ""}
                except errors.OperationFailure as err:
                    # log err here
                    return {"success": False, "message": "Order confirmation failed.", "data": ""}
        else:
            return {"success": False, "message": "Cart is empty. Cannot confirm order.", "data": ""}

    @staticmethod
    def cancel_order(order_id):
        """
        Cancels an order by updating its status to 'Cancelled'.
        Only works for orders that are 'Pending' or 'Confirmed'.
        """
        if not Order.order_exists(order_id):
            return {"success": False, "message": "Order does not exist.", "data": ""}

        try:
            doc = orders_table.find_one({"order_id": order_id})
            if not doc:
                return {"success": False, "message": "Order not found.", "data": ""}

            current_status = doc.get("status")

            # Business rule: an order can only be cancelled if it hasn't been processed yet.
            if current_status in ["Pending", "Confirmed"]:
                orders_table.update_one(
                    {"order_id": order_id},
                    {"$set": {"status": "Cancelled"}}
                )
                return {"success": True, "message": f"Order '{order_id}' cancelled successfully.", "data": ""}

            if current_status == "Cancelled":
                return {"success": False, "message": f"Order '{order_id}' is already cancelled.", "data": ""}

            # For anything else (In-Progress, In-Transit, etc.), it's too late.
            return {
                "success": False,
                "message": f"Order cannot be cancelled. Its status is '{current_status}'.",
                "data": ""
            }
        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": "Order cancellation failed.", "data": ""}

    @staticmethod
    def view_orders():
        """
        Retrieves all orders from the database, sorted by status.
        """
        try:
            pipeline = [
                {
                    "$addFields": {
                        "sort_order": {
                            "$switch": {
                                "branches": [
                                    {"case": {"$eq": ["$status", "Pending"]}, "then": 0},
                                    {"case": {"$eq": ["$status", "Confirmed"]}, "then": 1},
                                    {"case": {"$eq": ["$status", "In-Progress"]}, "then": 2},
                                    {"case": {"$eq": ["$status", "In-Transit"]}, "then": 3},
                                    {"case": {"$eq": ["$status", "Partially Fulfilled"]}, "then": 4},
                                    {"case": {"$eq": ["$status", "Fully Fulfilled"]}, "then": 5}
                                ],
                                "default": 6
                            }
                        }
                    }
                },
                {
                    "$sort": {
                        "sort_order": 1
                    }
                },
                {
                    "$project": {
                        "_id": 0,
                        "sort_order": 0
                    }
                }
            ]
            cursor = orders_table.aggregate(pipeline)
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No orders found.", "data": ""}
        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": "Failed to retrieve orders.", "data": ""}

    @staticmethod
    def view_order_products(order_id):
        """
        Fetches the list of products for a single, specific order.
        """
        try:
            if Order.order_exists(order_id):
                doc = orders_table.find_one({"order_id": order_id}, {"_id": 0})
                if doc and "products" in doc:
                    products = doc["products"]
                    if products:
                        return {"success": True, "message": "", "data": products}
                    else:
                        # The order exists but has an empty product list.
                        return {"success": False, "message": "No products found in this order.", "data": ""}
                else:
                    return {"success": False, "message": "Order has no products.", "data": ""}
            else:
                return {"success": False, "message": "Order does not exist.", "data": ""}
        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": "Failed to retrieve order products.", "data": ""}

    @staticmethod
    def download_orders_csv():
        """
        Exports all orders to a CSV formatted string.
        """
        try:
            data = list(orders_table.find({}, {"_id": 0}))
            if data:
                # Use pandas to easily convert the list of dicts to a CSV.
                df = pd.DataFrame(data)
                csv_buffer = io.StringIO()
                df.to_csv(csv_buffer, index=False)
                csv_content = csv_buffer.getvalue()
                return csv_content
            else:
                # No data to export.
                return ""
        except errors.OperationFailure as err:
            # log err
            pass

    @staticmethod
    def update_order_status(order_id, new_status):
        """
        Updates an order's status, ensuring it only moves forward in the lifecycle.
        """
        if not Order.order_exists(order_id):
            return {"success": False, "message": "Order does not exist.", "data": ""}

        try:
            doc = orders_table.find_one({"order_id": order_id})
            if not doc:
                return {"success": False, "message": "Order not found.", "data": ""}

            current_status = doc.get("status")

            # Rule 1: Cannot change a status that is considered "final".
            if current_status in ["Cancelled", "Partially Fulfilled", "Fully Fulfilled"]:
                return {
                    "success": False,
                    "message": f"Order is already '{current_status}' and its status cannot be changed.",
                    "data": ""
                }

            # Rule 2: Prevent moving backward (e.g., from 'In-Transit' back to 'Confirmed').
            current_level = Order.STATUS_ORDER.get(current_status, -1)
            new_level = Order.STATUS_ORDER.get(new_status, -1)

            if new_level <= current_level:
                return {
                    "success": False,
                    "message": f"Cannot update status from '{current_status}' back to '{new_status}'.",
                    "data": ""
                }

            # If all checks pass, apply the update.
            orders_table.update_one(
                {"order_id": order_id},
                {"$set": {"status": new_status}}
            )
            return {"success": True, "message": f"Order '{order_id}' status updated to '{new_status}'.", "data": ""}

        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": "Order status update failed.", "data": ""}

    @staticmethod
    def get_upcoming_deliveries():
        """
        Finds all active orders with an expected delivery date in the future.
        """
        try:
            today = date.today()
            upcoming_deliveries = []

            # Find all orders that are not yet finished or cancelled.
            cursor = orders_table.find(
                {"status": {"$nin": ["Fully Fulfilled", "Cancelled"]}},
                {"_id": 0}
            )

            for order in cursor:
                delivery_date_str = order.get("expect_delivery")
                if delivery_date_str:
                    # Convert the date string from the DB into a comparable date object.
                    delivery_date = datetime.strptime(delivery_date_str, "%d-%m-%Y").date()
                    if delivery_date > today:
                        upcoming_deliveries.append(order)

            if upcoming_deliveries:
                return {"success": True, "message": "Upcoming deliveries found.", "data": upcoming_deliveries}
            else:
                return {"success": False, "message": "No upcoming deliveries found.", "data": ""}

        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": "Failed to retrieve upcoming deliveries.", "data": ""}
        except Exception as err:
            # log a general error
            return {"success": False, "message": f"An unexpected error occurred", "data": ""}

    @staticmethod
    def order_exists(order_id):
        """
        A quick helper to check if an order with a given ID exists.
        """
        try:
            # find_one is more efficient than count_documents if I only need to know existence.
            doc = orders_table.find_one({"order_id": order_id})
            return True if doc else False
        except errors.OperationFailure as err:
            # log err
            return False

    @staticmethod
    def search_order(order_id):
        """
        Finds and returns a single order by its ID.
        """
        if Order.order_exists(order_id):
            try:
                doc = orders_table.find_one({"order_id": order_id}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Order not found.", "data": ""}
            except errors.OperationFailure as err:
                # log error
                return {"success": False, "message": "Failed to retrieve order.", "data": ""}
        else:
            return {"success": False, "message": "Order does not exist.", "data": ""}

if __name__ == "__main__":
    try:
        # This index ensures order_id is unique and speeds up searches.
        orders_table.create_index("order_id", unique=True)
    except errors.OperationFailure as e:
        pass