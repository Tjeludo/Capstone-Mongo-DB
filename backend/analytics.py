from datetime import datetime, timedelta
from pymongo import errors
from .connect import connect_to_mongodb

orders_table = connect_to_mongodb("orders")


class Analytics:
    @staticmethod
    def get_dashboard_metrics(days: int = 30):
        """
        Calculates several analytics metrics based on orders within a specified time period.
        Args:
            days (int): The number of days in the past to include in the calculation.
        """
        try:
            # Calculate the start date for the filtering
            start_date = datetime.now() - timedelta(days=days)

            # Base pipeline with date conversion and filtering
            base_pipeline = [
                {
                    "$addFields": {
                        "order_date_converted": {
                            "$dateFromString": {
                                "dateString": "$order_date",
                                "format": "%d-%m-%Y %H:%M"
                            }
                        }
                    }
                },
                {
                    "$match": {
                        "order_date_converted": {"$gte": start_date}
                    }
                }
            ]

            # --- 1. Most Ordered Products ---
            most_ordered_pipeline = base_pipeline + [
                {"$unwind": "$products"},
                {"$group": {
                    "_id": "$products.product_code",
                    "total_quantity": {"$sum": "$products.quantity"}
                }},
                {"$sort": {"total_quantity": -1}},
                {"$limit": 5},
                {"$project": {
                    "product_code": "$_id",
                    "total_quantity": 1,
                    "_id": 0
                }}
            ]
            most_ordered_products = list(orders_table.aggregate(most_ordered_pipeline))

            # --- 2. Order Status Distribution ---
            status_dist_pipeline = base_pipeline + [
                {"$group": {
                    "_id": "$status",
                    "count": {"$sum": 1}
                }},
                {"$project": {
                    "status": "$_id",
                    "count": 1,
                    "_id": 0
                }}
            ]
            order_status_distribution = list(orders_table.aggregate(status_dist_pipeline))

            # --- 3. Order Frequency by Supplier ---
            freq_by_supplier_pipeline = base_pipeline + [
                {"$group": {
                    "_id": "$supplier_id",
                    "order_count": {"$sum": 1}
                }},
                {"$sort": {"order_count": -1}},
                {"$project": {
                    "supplier_id": "$_id",
                    "order_count": 1,
                    "_id": 0
                }}
            ]
            order_frequency_by_supplier = list(orders_table.aggregate(freq_by_supplier_pipeline))

            # Combine all results into one response
            data = {
                "most_ordered_products": most_ordered_products,
                "order_status_distribution": order_status_distribution,
                "order_frequency_by_supplier": order_frequency_by_supplier,
            }

            return {"success": True, "message": "Analytics data retrieved successfully.", "data": data}

        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": f"Database operation failed: {err}", "data": {}}
        except Exception as e:
            # log err
            return {"success": False, "message": f"An unexpected error occurred: {e}", "data": {}}

