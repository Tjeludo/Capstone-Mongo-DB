from .connection import connect_MongoDB
# Assuming these connections are established correctly
delivery_table = connect_MongoDB("deliveries")
orders_table = connect_MongoDB("orders")
inventory_table = connect_MongoDB("Inventory")

class deliveryItem:
    def __init__(self, item_id, itemName, quantity, comments):
        self.item_id = item_id
        self.itemName = itemName
        self.quantity = quantity
        self.comments = comments
    
    def to_dict(self):
        return {
            "item_id": self.item_id,
            "itemName": self.itemName,
            "quantity": self.quantity,
            "comments": self.comments
        }

class Delivery:
    def __init__(self, deliveryNum, supplier_id, supplierName, order_id, date, checkedByWho, deliveryItems, comments):
        self.deliveryNum = deliveryNum
        self.supplier_id = supplier_id
        self.supplierName = supplierName
        self.order_id = order_id
        self.date = date
        self.checkedByWho = checkedByWho
        self.deliveryItems = deliveryItems # This is a list of deliveryItem objects
        self.comments = comments
    
    def add_delivery_item(self):
        """Inserts the new delivery record into the database."""
        doc = {
            "delivery_num": self.deliveryNum,
            "supplier_id": self.supplier_id,
            "supplier name": self.supplierName,
            "order_id": self.order_id,
            "date": self.date,
            "checkedByWho": self.checkedByWho,
            "deliveryItems": [item.to_dict() for item in self.deliveryItems],
            "comments": self.comments
        }
        try:
            delivery_table.insert_one(doc)
            print(f"Delivery {self.deliveryNum} created successfully.")
            return True
        except Exception as e:
            print(f"Error occurred. Couldn't add delivery: {e}")
            return False

    def update_order_status(self):
        """
        Updates the associated order's status based on its current state and the
        cumulative deliveries received.
        """
        #  Find the corresponding order.
        order = orders_table.find_one({"order_id": self.order_id})
        if not order:
            print(f"Error: Order ID {self.order_id} not found.")
            return

        current_status = order.get("status")

        # Handle orders that are already fully fulfilled.
        if current_status == "Fully Fulfilled":
            print(f"Failed: Cannot add delivery for order {self.order_id} because it is already fully fulfilled.")
            #Clean up the delivery document that was just created
            delivery_table.delete_one({"delivery_num": self.deliveryNum})
            print(f"Reverted creation of delivery {self.deliveryNum}.")
            return

        #Calculate total required vs. total delivered quantities.
        
        required_map = {
            item["product_code"]: {"name": item["description"], "quantity": item["quantity"]}
            for item in order["products"]
        }

        # Find ALL deliveries for this order to get a cumulative total.
        all_deliveries = delivery_table.find({"order_id": self.order_id})
        total_delivered_map = {}
        for delivery_doc in all_deliveries:
            for item in delivery_doc.get("deliveryItems", []):
                item_id = item["item_id"]
                quantity = item["quantity"]
                total_delivered_map[item_id] = total_delivered_map.get(item_id, 0) + quantity

        #  Determine the new status and list of still-missing items.
        still_missing_items = []
        is_now_fully_fulfilled = True

        for item_code, required_data in required_map.items():
            required_qty = required_data["quantity"]
            delivered_qty = total_delivered_map.get(item_code, 0)

            if delivered_qty < required_qty:
                is_now_fully_fulfilled = False
                still_missing_items.append({
                    "itemcode": item_code,
                    "itemName": required_data["name"],
                    "missing_quantity": required_qty - delivered_qty
                })
        
        # Update the order and the new delivery document.
        new_status = "Fully Fulfilled" if is_now_fully_fulfilled else "Partially Fulfilled"

        # Update the order's status
        orders_table.update_one(
            {"order_id": self.order_id},
            {"$set": {"status": new_status}}
        )
        print(f"Order {self.order_id} status updated to '{new_status}'.")

        # Update the current delivery with the list of what is still missing overall.
        if still_missing_items:
            delivery_table.update_one(
                {"delivery_num": self.deliveryNum},
                {"$set": {"missing items": still_missing_items}}
            )
        else:
            # If nothing is missing, we can remove the field to keep docs clean.
            delivery_table.update_one(
                {"delivery_num": self.deliveryNum},
                {"$unset": {"missing items": ""}}
            )

    def update_main_branch_inventory(self):
        for item in self.deliveryItems:
            inventory_table.update_one(
                {"product_code": item.item_id, "branchID": "B001"},
                {"$inc": {"quantity": item.quantity}},
                upsert=True # Creates the item in inventory if it doesn't exist #Need to do a validation check before this
            )
            print(f"Updated inventory for item {item.item_id} at main branch with quantity {item.quantity}")

    
    def recent_deliveries(self):
        deliveries = delivery_table.find().sort("date", -1).limit(5)
        for delivery in deliveries:
            print(f"Delivery Number: {delivery['delivery_num']}, Supplier: {delivery['supplier name']}, Date: {delivery['date']}")
            for item in delivery['deliveryItems']:
                print(f"  Item ID: {item['item_id']}, Quantity: {item['quantity']}")
            if "missing items" in delivery:
                print(f"  Overall Missing Items: {delivery['missing items']}")
            print("-" * 20)
    
    @staticmethod
    def get_del_number():
        last_delivery = delivery_table.find_one(sort=[("delivery_num", -1)])
        if last_delivery and last_delivery.get("delivery_num", "").startswith("DEL"):
            last_num = int(last_delivery["delivery_num"][3:])
            next_num = last_num + 1
            return f"DEL{next_num:03d}"
        return "DEL001"