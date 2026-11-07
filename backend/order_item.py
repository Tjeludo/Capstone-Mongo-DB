class OrderItem:
    def __init__(self, product_code, price, quantity):
        self.product_code = product_code
        self.price = price
        self.quantity = quantity
        self.subtotal = self.price * self.quantity

    # ! Create
    def create_order_product(self):
        return {
            "product_code": self.product_code,
            "price(ZAR)": round(self.price, 2),
            "quantity": self.quantity,
            "subtotal": round(self.subtotal, 2)}