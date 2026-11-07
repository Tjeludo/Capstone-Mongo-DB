from .connect import connect_to_mongodb
from pymongo import errors
from tabulate import tabulate

sizes_table = connect_to_mongodb("sizes")


class Size:
    def __init__(self, name, code):
        self.name = name
        self.code = code

    def add_size(self):
        doc = {
            "size_name": self.name,
            "size_code": self.code}
        try:
            if sizes_table.find_one({"$or": [{"size_code": self.code}, {"size_name": self.name}]}):
                print("[INFO] Size already exists")
            else:
                sizes_table.insert_one(doc)
                print(f"[SUCCESS] Size '{self.code}' created.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Size creation failed: {e}")

    @staticmethod
    def view_sizes():
        try:
            sizes = list(sizes_table.find({}, {"_id": 0}))
            if sizes:
                headers = sizes[0].keys()
                rows = [list(size.values()) for size in sizes]
                print(tabulate(rows, headers, tablefmt="simple"))
            else:
                print("[INFO] No sizes found.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve products: {e}")

    @staticmethod
    def get_size_id(size_name):
        try:
            size = sizes_table.find_one({"size_name": size_name})
            if size:
                return size.get("size_code")
            return None
        except Exception as e:
            print(f"[ERROR] Failed to retrieve product: {e}")


if __name__ == "__main__":
    # Size initialization data
    size_codes = {
        0: "none", 1: "newborn", 2: "0-3 months", 3: "3-6 months",
        4: "6-12 months", 5: "12-18 months", 6: "18-24 months",
        7: "2-3 years", 8: "3-4 years", 9: "4-5 years",}

    for code, name in size_codes.items():
        Size(name, code).add_size()