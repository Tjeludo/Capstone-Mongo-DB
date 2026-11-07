# backend/home.py
from datetime import datetime, timedelta
from pymongo import errors
from .connect import connect_to_mongodb

# Establish connections to the necessary collections
orders_table = connect_to_mongodb("orders")
deliveries_table = connect_to_mongodb("deliveries")
transfers_table = connect_to_mongodb("Transfers")
message_requests_table = connect_to_mongodb("message_requests")


class Home:
    @staticmethod
    def get_pending_requests_count():
        """Counts all message requests with a 'Pending' status."""
        try:
            count = message_requests_table.count_documents({"status": "Pending"})
            return {"success": True, "count": count}
        except errors.OperationFailure as e:
            return {"success": False, "message": f"Database error: {e}", "count": 0}

    @staticmethod
    def get_dashboard_metrics():
        """
        Calculates all dashboard metrics for the home page using efficient database aggregations.
        """
        try:
            # Define time ranges
            today = datetime.now()
            start_of_month = today.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
            start_of_week = today - timedelta(days=today.weekday())
            start_of_week = start_of_week.replace(hour=0, minute=0, second=0, microsecond=0)

            # --- 1. Total Expenses for Current Month ---
            expenses_pipeline = [
                {"$addFields": {"order_date_converted": {
                    "$dateFromString": {"dateString": "$order_date", "format": "%d-%m-%Y %H:%M"}}}},
                {"$match": {"order_date_converted": {"$gte": start_of_month}}},
                {"$group": {"_id": None, "total": {"$sum": "$total_cost(R)"}}}
            ]
            expenses_result = list(orders_table.aggregate(expenses_pipeline))
            total_expenses = expenses_result[0]['total'] if expenses_result else 0

            # --- 2. Order Counts (Pending & Confirmed) for Current Week ---
            orders_pipeline = [
                {"$addFields": {"order_date_converted": {
                    "$dateFromString": {"dateString": "$order_date", "format": "%d-%m-%Y %H:%M"}}}},
                {"$match": {"order_date_converted": {"$gte": start_of_week}}},
                {"$group": {"_id": "$status", "count": {"$sum": 1}}}
            ]
            orders_result = list(orders_table.aggregate(orders_pipeline))
            pending_orders = next((item['count'] for item in orders_result if item['_id'] == 'Pending'), 0)
            confirmed_orders = next((item['count'] for item in orders_result if item['_id'] == 'Confirmed'), 0)

            # --- 3. Deliveries Count for Current Week ---
            deliveries_pipeline = [
                {"$addFields": {
                    "delivery_date_converted": {"$dateFromString": {"dateString": "$date", "format": "%Y-%m-%d"}}}},
                {"$match": {"delivery_date_converted": {"$gte": start_of_week}}},
                {"$count": "count"}
            ]
            deliveries_result = list(deliveries_table.aggregate(deliveries_pipeline))
            deliveries_count = deliveries_result[0]['count'] if deliveries_result else 0

            # --- 4. Transfers Count for Current Week ---
            transfers_pipeline = [
                {"$addFields": {"transfer_date_converted": {
                    "$dateFromString": {"dateString": "$date", "format": "%d-%m-%Y %H:%M"}}}},
                {"$match": {"transfer_date_converted": {"$gte": start_of_week}}},
                {"$count": "count"}
            ]
            transfers_result = list(transfers_table.aggregate(transfers_pipeline))
            transfers_count = transfers_result[0]['count'] if transfers_result else 0

            # --- 5. Pending Requests Count ---
            pending_requests_result = Home.get_pending_requests_count()
            pending_requests_count = pending_requests_result['count'] if pending_requests_result['success'] else 0

            # --- Combine all results into one response ---
            data = {
                "total_expenses": total_expenses,
                "pending_orders": pending_orders,
                "confirmed_orders": confirmed_orders,
                "deliveries_count": deliveries_count,
                "transfers_count": transfers_count,
                "pending_requests_count": pending_requests_count
            }

            return {"success": True, "message": "Dashboard data retrieved successfully.", "data": data}

        except errors.OperationFailure as err:
            return {"success": False, "message": f"Database operation failed: {err}", "data": {}}
        except Exception as e:
            return {"success": False, "message": f"An unexpected error occurred: {e}", "data": {}}