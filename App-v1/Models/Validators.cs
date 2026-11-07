using System;
using System.Text.RegularExpressions;

namespace App_v1.Helpers
{
    public static class Validators
    {
        public static string ValidateRole(string role)
        {
            string pattern = @"^[A-Za-z]{2,3}$";
            if (!string.IsNullOrWhiteSpace(role))
            {
                role = role.Trim().ToUpper();
                if (Regex.IsMatch(role, pattern))
                    return role;
            }
            throw new ArgumentException("Invalid role.");
        }

        public static string ValidateBranchCode(string code)
        {
            string pattern = @"^B\d{2}$";
            if (!string.IsNullOrWhiteSpace(code))
            {
                code = code.Trim().ToUpper();
                if (Regex.IsMatch(code, pattern))
                    return code;
            }
            throw new ArgumentException("Invalid branch code.");
        }

        public static string ValidateCategoryCode(string code)
        {
            string pattern = @"^C\d{2}$";
            if (!string.IsNullOrWhiteSpace(code))
            {
                code = code.Trim().ToUpper();
                if (Regex.IsMatch(code, pattern))
                    return code;
            }
            throw new ArgumentException("Invalid category code.");
        }

        public static string ValidateItemCode(string code)
        {
            string pattern = @"^0\d{9}$";
            if (!string.IsNullOrWhiteSpace(code))
            {
                code = code.Trim();
                if (Regex.IsMatch(code, pattern))
                    return code;
            }
            throw new ArgumentException("Invalid item code.");
        }

        public static string ValidateOrderId(string orderId)
        {
            string pattern = @"^ODR\d{3}$";
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                orderId = orderId.Trim().ToUpper();
                if (Regex.IsMatch(orderId, pattern))
                    return orderId;
            }
            throw new ArgumentException("Invalid order id.");
        }

        public static string ValidateSupplierId(string supplierId)
        {
            string pattern = @"^S\d{2}$";
            if (!string.IsNullOrWhiteSpace(supplierId))
            {
                supplierId = supplierId.Trim().ToUpper();
                if (Regex.IsMatch(supplierId, pattern))
                    return supplierId;
            }
            throw new ArgumentException("Invalid supplier id.");
        }

        public static string ValidateStaffId(string staffId)
        {
            string pattern = @"^STF-\d{6}$";
            if (!string.IsNullOrWhiteSpace(staffId))
            {
                staffId = staffId.Trim().ToUpper();
                if (Regex.IsMatch(staffId, pattern))
                    return staffId;
            }
            throw new ArgumentException("Invalid staff id.");
        }

        public static string ValidateStaffEmail(string email)
        {
            string pattern = @"^[A-Za-z0-9._%+-]{2,100}@baby-bargains\.co\.za$";
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim().ToLower();
                if (Regex.IsMatch(email, pattern))
                    return email;
            }
            throw new ArgumentException("Invalid staff email.");
        }

        public static string ValidateCategoryName(string name)
        {
            string pattern = @"^[A-Za-z0-9 &\-/'.]{2,50}$";
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                if (Regex.IsMatch(name, pattern))
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            }
            throw new ArgumentException("Invalid category name.");
        }

        public static string ValidateDescription(string descript)
        {
            if (!string.IsNullOrWhiteSpace(descript))
            {
                descript = descript.Trim().ToLower();
                if (descript.Length <= 255)
                    return descript;
                throw new ArgumentException("Description must be 0-255 characters.");
            }
            throw new ArgumentException("Description must be a string.");
        }

        public static string ValidateItemName(string name)
        {
            string pattern = @"^[A-Za-z0-9 &\-/'.]{2,50}$";
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                if (Regex.IsMatch(name, pattern))
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            }
            throw new ArgumentException("Invalid item name.");
        }

        public static string ValidateItemSize(string size)
        {
            string[] sizeCodes = { "none", "newborn", "0-3 months", "3-6 months", "6-12 months", "12-18 months", "18-24 months", "2-3 years", "3-4 years", "4-5 years" };
            if (!string.IsNullOrWhiteSpace(size))
            {
                size = size.Trim().ToLower();
                foreach (var s in sizeCodes)
                    if (size == s)
                        return size;
            }
            throw new ArgumentException("Invalid size.");
        }

        public static string ValidateItemColour(string colour)
        {
            string pattern = @"^[A-Za-z]{3,25}$";
            if (!string.IsNullOrWhiteSpace(colour))
            {
                colour = colour.Trim();
                if (Regex.IsMatch(colour, pattern))
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(colour.ToLower());
            }
            throw new ArgumentException("Invalid colour.");
        }

        public static string ValidateName(string name)
        {
            string pattern = @"^[A-Za-z '-]{2,50}$";
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                if (Regex.IsMatch(name, pattern))
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            }
            throw new ArgumentException("Invalid name.");
        }

        public static string ValidateEmail(string email)
        {
            string pattern = @"^[A-Za-z0-9._%+-]{2,100}@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim().ToLower();
                if (Regex.IsMatch(email, pattern))
                    return email;
            }
            throw new ArgumentException("Invalid email.");
        }

        public static string ValidatePhoneNumber(string phone)
        {
            string pattern = @"^\d{10}$";
            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = phone.Trim();
                if (Regex.IsMatch(phone, pattern))
                    return phone;
            }
            throw new ArgumentException("Invalid phone number.");
        }

        public static string ValidateGender(string gender)
        {
            string[] genders = { "Male", "Female", "Other" };
            if (!string.IsNullOrWhiteSpace(gender))
            {
                gender = gender.Trim();
                foreach (var g in genders)
                    if (string.Equals(gender, g, StringComparison.OrdinalIgnoreCase))
                        return g;
            }
            throw new ArgumentException("Invalid gender.");
        }

        public static string ValidateAddress(string address)
        {
            string pattern = @"^[A-Za-z0-9 ,\./'-]{5,100}$";
            if (!string.IsNullOrWhiteSpace(address))
            {
                address = address.Trim();
                if (Regex.IsMatch(address, pattern))
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(address.ToLower());
            }
            throw new ArgumentException("Invalid address.");
        }

        public static double ValidatePrice(object price)
        {
            double priceValue;
            if (price is string s)
            {
                if (!double.TryParse(s, out priceValue))
                    throw new ArgumentException("Price must be a number.");
            }
            else if (price is double d)
                priceValue = d;
            else if (price is int i)
                priceValue = i;
            else
                throw new ArgumentException("Invalid price.");

            if (priceValue < 0 || priceValue > 10000)
                throw new ArgumentException("Price out of range.");

            return Math.Round(priceValue, 2);
        }

        public static int ValidateQuantity(object quantity)
        {
            int qtyValue;
            if (quantity is string s)
            {
                if (!int.TryParse(s, out qtyValue))
                    throw new ArgumentException("Quantity must be an integer.");
            }
            else if (quantity is int i)
                qtyValue = i;
            else
                throw new ArgumentException("Invalid quantity.");

            if (qtyValue < 0)
                throw new ArgumentException("Quantity cannot be negative.");

            return qtyValue;
        }

        public static string ValidateFileName(string file)
        {
            string pattern = @".+\.(csv|xls|xlsx)$";
            if (!string.IsNullOrWhiteSpace(file))
            {
                file = file.Trim();
                if (Regex.IsMatch(file, pattern, RegexOptions.IgnoreCase))
                    return file;
            }
            throw new ArgumentException("Invalid file name.");
        }

        public static string ValidateStatus(string status)
        {
            string[] allowedStatuses = { "pending", "draft", "processing", "confirmed", "in-transit", "delivered", "completed", "cancelled", "returned", "active", "inactive" };
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim().ToLower();
                foreach (var s in allowedStatuses)
                    if (s == status)
                        return status;
            }
            throw new ArgumentException("Invalid status.");
        }

        public static string ValidatePassword(string password)
        {
            if (!string.IsNullOrWhiteSpace(password))
                return password.Trim();

            throw new ArgumentException("Invalid password.");
        }
    }
}
