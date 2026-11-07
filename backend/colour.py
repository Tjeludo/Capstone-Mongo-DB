from .connect import connect_to_mongodb
from pymongo import errors
from tabulate import tabulate

colours_table = connect_to_mongodb("colours")

class Colour:
    def __init__(self, colour_name, colour_code):
        self.name = colour_name
        self.code = colour_code

    def add_colour(self):
        doc = {
            "colour_name": self.name,
            "colour_code": self.code
        }
        try:
            if colours_table.find_one({"$or": [{"colour_code": self.code}, {"colour_name": self.name}]}):
                print("Colour already exists")
            else:
                colours_table.insert_one(doc)
                print(f"[SUCCESS] Colour '{self.code}' created.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Colour creation failed: {e}")

    @staticmethod
    def view_colours():
        try:
            colours = list(colours_table.find({}, {"_id": 0}))
            if colours:
                headers = colours[0].keys()
                rows = [list(colour.values()) for colour in colours]
                print(tabulate(rows, headers, tablefmt="simple"))
            else:
                print("[INFO] No colours found.")
        except errors.OperationFailure as e:
            print(f"[ERROR] Failed to retrieve products: {e}")

    @staticmethod
    def get_colour_id(colour_name):
        try:
            colour = colours_table.find_one({"colour_name": colour_name})
            if colour:
                return colour.get("colour_code")
            return None
        except Exception as e:
            print(f"[ERROR] Failed to retrieve product: {e}")

if __name__ == "__main__":
    # Colour initialization data
    colour_codes = {
        1: "Red", 2: "Blue", 3: "Yellow", 4: "Green", 5: "Orange", 6: "Purple",
        7: "Black", 8: "White", 9: "Grey", 10: "Light Grey", 11: "Navy", 12: "Beige",
        13: "Brown", 14: "Cream", 15: "Charcoal", 16: "Silver", 17: "Gold", 18: "Peach",
        19: "Sky Blue", 20: "Lilac", 21: "Sea Green", 22: "Olive", 23: "Burgundy",
        24: "Maroon", 25: "Hot Pink", 26: "Turquoise", 27: "Teal", 28: "Cyan",
        29: "Magenta", 30: "Chocolate", 31: "Rose", 32: "Mustard", 33: "Pink",
        34: "Light Blue", 35: "Aqua"
    }

    for code, name in colour_codes.items():
        Colour(name, code).add_colour()