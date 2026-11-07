from .connection import connect_MongoDB
stock_take_table = connect_MongoDB("stock_take")
inventory_table = connect_MongoDB("Inventory")

class StockTakeItem:
    def __init__(self, itemID, itemName, previousQuantity, quantityOnHand): #Dont think i should add the other variables, it just seems redundant. cause this the itemID it can give you the descriptions
        self.itemID = itemID
        self.itemName = itemName
        self.previousQuantity = previousQuantity
        self.quantityOnHand = quantityOnHand
    
    def to_dict(self):
        return {
            "itemID": self.itemID,
            "itemName": self.itemName,
            "previousQuantity": self.previousQuantity,
            "quantityOnHand": self.quantityOnHand,
        }
    


class StockTake:
    def __init__(self, stockTakeNum, branchID, date, items):
        self.stockTakeNum = stockTakeNum
        self.branchID = branchID
        self.date = date
        self.items = items

    def to_dict(self):
        return {
            "stockTakeNum": self.stockTakeNum,
            "branchID": self.branchID,
            "date": self.date,
            "items": [item.to_dict() for item in self.items]
        }
    @staticmethod
    def get_next_stock_take_number():
        # Find the highest stockTakeNum in the collection
        last_stock_take = stock_take_table.find_one(
            sort=[("stockTakeNum", -1)]
        )
        if last_stock_take and last_stock_take.get("stockTakeNum", "").startswith("ST"):
            last_num = int(last_stock_take["stockTakeNum"][2:])
        else:
            last_num = 0
        next_num = last_num + 1
        return f"ST{next_num:03d}"
    
    
    def update_stock_levels(self):
        for item in self.items:
            inventory_table.update_one(
                {"product_code": item.itemID, "branchID": self.branchID},
                {"$set": {
                    "quantity": item.quantityOnHand,
                }},
                upsert=True   #Update if exists, insert if not
                
            )
            print(f"Updated stock level for item {item.itemID} in branch {self.branchID} to {item.quantityOnHand}") 
    def add_stock_take(self):
        doc = {
            "stockTakeNum": self.stockTakeNum,
            "branchID": self.branchID,
            "date": self.date,
            "items": [item.to_dict() for item in self.items]
        }
        try:
            stock_take_table.insert_one(doc)
            print("Stock take added successfully")
        except Exception as e:
            print(f"Error occurred while adding stock take: {e}")
            
    #Gives the log of all stock takes for a specific branch
    def view_stock_takes(self,branchID):
        self.branchID = branchID
        stock_takes = stock_take_table.find({"branchID": self.branchID})
        for stock_take in stock_takes:
            print(f"Stock Take Number: {stock_take['stockTakeNum']}, Date: {stock_take['date']}")
            for item in stock_take['items']:
                print(f"Item ID: {item['itemID']}, Quantity On Hand: {item['quantityOnHand']}")
            print("\n")
    
    
    
