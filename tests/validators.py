import re


class Validation:

    @staticmethod
    def validate_role(role):
        pattern = r"[A-Za-z]{2,3}"
        if isinstance(role, str):
            role = role.strip().upper()
            if re.fullmatch(pattern, role):
                return role
        raise ValueError("Invalid role.")

    @staticmethod
    def validate_branch_code(code):
        pattern = r"B\d{2}"
        if isinstance(code, str):
            code = code.strip().upper()
            if re.fullmatch(pattern, code):
                return code
        raise ValueError("Invalid branch code.")

    @staticmethod
    def validate_category_code(code):
        pattern = r"C\d{2}"
        if isinstance(code, str):
            code = code.strip().upper()
            if re.fullmatch(pattern, code):
                return code
        raise ValueError("Invalid category code.")

    @staticmethod
    def validate_product_code(code):
        pattern = r"0\d{9}"
        if isinstance(code, str):
            code = code.strip()
            if re.fullmatch(pattern, code):
                return code
        raise ValueError("Invalid product code.")

    @staticmethod
    def validate_order_id(order_id):
        pattern = r"ODR\d{3}"
        if isinstance(order_id, str):
            order_id = order_id.strip().upper()
            if re.fullmatch(pattern, order_id):
                return order_id
        raise ValueError("Invalid order id.")

    @staticmethod
    def validate_supplier_id(supplier_id):
        pattern = r"S\d{2}"
        if isinstance(supplier_id, str):
            supplier_id = supplier_id.strip().upper()
            if re.fullmatch(pattern, supplier_id):
                return supplier_id
        raise ValueError("Invalid supplier id.")

    @staticmethod
    def validate_staff_id(staff_id):
        pattern = r"STF-\d{6}"
        if isinstance(staff_id, str):
            staff_id = staff_id.strip().upper()
            if re.fullmatch(pattern, staff_id):
                return staff_id
        raise ValueError("Invalid staff id.")

    @staticmethod
    def validate_guest_id(guest_id):
        pattern = r"GST-\d{6}"
        if isinstance(guest_id, str):
            guest_id = guest_id.strip().upper()
            if re.fullmatch(pattern, guest_id):
                return guest_id
        raise ValueError("Invalid guest id.")

    @staticmethod
    def validate_staff_email(email):
        pattern = r"[A-Za-z0-9._%+-]{2,100}@baby-bargains\.co\.za"
        if isinstance(email, str):
            email = email.strip().lower()
            if re.fullmatch(pattern, email):
                return email
        raise ValueError("Invalid staff email.")

    @staticmethod
    def validate_cat_name(name):
        pattern = r"[A-Za-z0-9 &\-/'.]{2,50}"
        if isinstance(name, str):
            name = name.strip().title()
            if re.fullmatch(pattern, name):
                return name
        raise ValueError("Invalid category name.")

    @staticmethod
    def validate_description(description):
        if isinstance(description, str):
            description = description.strip()
            if 0 <= len(description) <= 255:
                return description
            raise ValueError("Description must be 0-255 characters.")
        raise ValueError("Description must be a string.")

    @staticmethod
    def validate_product_name(name):
        pattern = r"[A-Za-z0-9 &\-/'.]{2,50}"
        if isinstance(name, str):
            name = name.strip().title()
            if re.fullmatch(pattern, name):
                return name
        raise ValueError("Invalid product name.")

    @staticmethod
    def validate_product_size(size):
        size_codes = ["none", "newborn", "0-3 months", "3-6 months", "6-12 months", "12-18 months", "18-24 months",
                      "2-3 years", "3-4 years", "4-5 years"]
        if isinstance(size, str):
            size = size.strip().lower()
            if size in size_codes:
                return size
        raise ValueError("Invalid size.")

    @staticmethod
    def validate_product_colour(colour):
        pattern = r"[A-Za-z ]{3,25}"
        if isinstance(colour, str):
            colour = colour.strip().title()
            if re.fullmatch(pattern, colour):
                return colour
        raise ValueError("Invalid colour.")

    @staticmethod
    def validate_name(name):
        pattern = r"[A-Za-z '-]{2,50}"
        if isinstance(name, str):
            name = name.strip().title()
            if re.fullmatch(pattern, name):
                return name
        raise ValueError("Invalid name/surname.")

    @staticmethod
    def validate_email(email):
        if isinstance(email, str):
            email = email.strip().lower()
            pattern = r"[A-Za-z0-9._%+-]{2,100}@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"
            if re.fullmatch(pattern, email):
                return email
        raise ValueError("Invalid email.")

    @staticmethod
    def validate_phone_number(phone):
        if isinstance(phone, str):
            phone = phone.strip()
            pattern = r"[\d\-\+\(\) ]{10,15}"  # More flexible phone pattern
            if re.fullmatch(pattern, phone):
                return phone
        raise ValueError("Invalid phone number.")

    @staticmethod
    def validate_date(date):
        # Accept various date formats
        if isinstance(date, str):
            date = date.strip()
            # Basic date validation - can be enhanced
            patterns = [
                r"\d{4}-\d{2}-\d{2}",  # YYYY-MM-DD
                r"\d{2}-\d{2}-\d{4}",  # DD-MM-YYYY
                r"\d{2}/\d{2}/\d{4}",  # DD/MM/YYYY
                r"\d{4}/\d{2}/\d{2}"  # YYYY/MM/DD
            ]
            for pattern in patterns:
                if re.fullmatch(pattern, date):
                    return date
        raise ValueError("Invalid date format.")

    @staticmethod
    def validate_gender(gender):
        genders = {"male", "female", "other"}
        if isinstance(gender, str):
            gender = gender.strip().lower()
            if gender in genders:
                return gender.title()
        raise ValueError("Invalid gender.")

    @staticmethod
    def validate_address(address):
        pattern = r"[A-Za-z0-9 ,\./'-]{5,100}"
        if isinstance(address, str):
            address = address.strip().title()
            if re.fullmatch(pattern, address):
                return address
        raise ValueError("Invalid address.")

    @staticmethod
    def validate_price(price):
        if isinstance(price, (int, float, str)):
            try:
                price_value = float(price)
            except ValueError:
                raise ValueError("Price must be a positive number")

            if 0 <= price_value <= 50000:  # Increased max for expensive items
                return round(price_value, 2)
        raise ValueError("Invalid price.")

    @staticmethod
    def validate_quantity(quantity):
        if isinstance(quantity, (int, str)):
            try:
                qty_value = int(quantity)
            except ValueError:
                raise ValueError("Quantity must be a positive integer")

            if qty_value >= 0:
                return qty_value
        raise ValueError("Invalid quantity.")

    @staticmethod
    def validate_filename(file):
        pattern = r".+\.(csv|xls|xlsx)$"
        if isinstance(file, str):
            file_name = file.strip()
            if re.fullmatch(pattern, file_name, re.IGNORECASE):
                return file_name
        raise ValueError("Invalid file name.")

    @staticmethod
    def validate_status(status):
        allowed_statuses = [
            "pending",
            "draft",
            "processing",
            "confirmed",
            "in-transit",
            "delivered",
            "completed",
            "cancelled",
            "returned",
            "active",
            "inactive"]

        if isinstance(status, str):
            status = status.strip().lower()
            if status in allowed_statuses:
                return status
        raise ValueError("Invalid status")

    @staticmethod
    def validate_password(password):
        if isinstance(password, str):
            password = password.strip()
            if password and len(password) >= 8:  # Minimum length
                return password
        raise ValueError("Invalid password.")

    @staticmethod
    def validate_branch_manager(manager):
        return Validation.validate_name(manager)

    @staticmethod
    def validate_branch_city(city):
        pattern = r"[A-Za-z '-]{2,50}"
        if isinstance(city, str):
            city = city.strip().title()
            if re.fullmatch(pattern, city):
                return city
        raise ValueError("Invalid city name.")

    @staticmethod
    def validate_colour_name(colour_name):
        return Validation.validate_product_colour(colour_name)

    @staticmethod
    def validate_size_name(size_name):
        return Validation.validate_product_size(size_name)