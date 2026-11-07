import pytest
from unittest.mock import patch, MagicMock
from datetime import datetime, timedelta
from backend.user import User
from pymongo import errors


# --- Fixtures ---

@pytest.fixture
def mock_users_table():
    """Mocks the 'users_table' database collection."""
    with patch("backend.user.users_table") as mock:
        yield mock


@pytest.fixture
def mock_staff_table():
    """Mocks the 'staff_table' database collection."""
    with patch("backend.user.staff_table") as mock:
        yield mock


@pytest.fixture
def mock_roles_table():
    """Mocks the 'roles_table' database collection."""
    with patch("backend.user.roles_table") as mock:
        yield mock


@pytest.fixture
def mock_bcrypt():
    """Mocks the 'bcrypt' library for password hashing and checking."""
    with patch("backend.user.bcrypt") as mock:
        # Simulate checkpw returning True by default
        mock.checkpw.return_value = True
        # Simulate hashpw returning a dummy hash
        mock.hashpw.return_value = b"hashed_password"
        yield mock


@pytest.fixture
def mock_smtp():
    """Mocks the 'SMTP' client to prevent sending real emails."""
    with patch("backend.user.SMTP") as mock:
        yield mock


@pytest.fixture(autouse=True)
def auto_mock_db_tables(mock_users_table, mock_staff_table, mock_roles_table, mock_bcrypt, mock_smtp):
    """A special fixture to automatically apply common mocks to all tests."""
    pass


# --- Test Cases ---

def test_add_user_success(mock_users_table):
    """Tests successful user creation."""
    user = User("test@email.com", "STF-01", "ValidPassword123!")
    result = user.add_user()
    assert result["success"] is True
    mock_users_table.insert_one.assert_called_once()


def test_add_user_duplicate(mock_users_table):
    """Tests failure when adding a user that already exists."""
    mock_users_table.insert_one.side_effect = errors.DuplicateKeyError("Duplicate key error")
    user = User("test@email.com", "STF-01", "ValidPassword123!")
    result = user.add_user()
    assert result["success"] is False
    assert "already exists" in result["message"]


def test_delete_user_success(mock_users_table):
    """Tests successful user deletion."""
    mock_users_table.delete_one.return_value = MagicMock(deleted_count=1)
    result = User.delete_user("STF-01")
    assert result["success"] is True


def test_delete_user_not_found(mock_users_table):
    """Tests deletion failure when user is not found."""
    mock_users_table.delete_one.return_value = MagicMock(deleted_count=0)
    result = User.delete_user("STF-99")
    assert result["success"] is False
    assert "not found" in result["message"]


def test_log_in_success(mock_users_table, mock_staff_table, mock_smtp):
    """Tests the first part of a successful login (password check and 2FA trigger)."""
    # Mock the user found in the users table with a hashed password
    mock_users_table.find_one.return_value = {"password": "hashed_password"}
    # Mock the staff member found in the staff table for sending the email
    mock_staff_table.find_one.return_value = {"email": "personal@email.com", "firstname": "John"}

    result = User.log_in("STF-01", "password123")

    assert result["success"] is True
    assert result["message"] == "2FA required"
    # Check that an OTP was generated and set in the database
    mock_users_table.update_one.assert_called_once()
    # Check that the email function was triggered
    mock_smtp.assert_called_once()


def test_log_in_failure_wrong_password(mock_users_table, mock_bcrypt):
    """Tests login failure due to an incorrect password."""
    mock_users_table.find_one.return_value = {"password": "hashed_password"}
    mock_bcrypt.checkpw.return_value = False  # Simulate password check failing
    result = User.log_in("STF-01", "wrong_password")
    assert result["success"] is False
    assert "Incorrect username/password" in result["message"]


def test_verify_2fa_success(mock_users_table, mock_staff_table, mock_roles_table):
    """Tests successful 2FA verification and session creation."""
    otp_expiry_time = datetime.now() + timedelta(minutes=5)
    mock_users_table.find_one.return_value = {"otp_code": "123456", "otp_expiry": otp_expiry_time}
    mock_staff_table.find_one.return_value = {"staff_id": "STF-01", "role": "MAN"}
    mock_roles_table.find_one.return_value = {"role_name": "Manager", "permissions": ["read", "write"]}

    result = User.verify_2fa("STF-01", "123456")

    assert result["success"] is True
    assert result["message"] == "Login successful"
    assert result["data"]["role"] == "Manager"
    assert "write" in result["data"]["permissions"]
    # Verify the OTP is cleared after successful use
    mock_users_table.update_one.assert_called_with({"staff_id": "STF-01"},
                                                   {"$unset": {"otp_code": "", "otp_expiry": ""}})


def test_verify_2fa_expired_code(mock_users_table):
    """Tests 2FA verification failure with an expired OTP code."""
    otp_expiry_time = datetime.now() - timedelta(minutes=5)  # Code expired 5 minutes ago
    mock_users_table.find_one.return_value = {"otp_code": "123456", "otp_expiry": otp_expiry_time}
    result = User.verify_2fa("STF-01", "123456")
    assert result["success"] is False
    assert "has expired" in result["message"]


def test_set_password_valid():
    """Tests that a valid password returns a hash."""
    # Bcrypt is mocked, so we just check that a value is returned
    result = User.set_password("ValidPassword123!")
    assert result is not None


def test_set_password_invalid():
    """Tests that an invalid (too short) password returns None."""
    result = User.set_password("short")
    assert result is None