import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from datetime import datetime
from backend.staff import Staff


# --- Fixtures ---

@pytest.fixture
def mock_staff_table():
    """Mocks the 'staff_table' database collection."""
    with patch("backend.staff.staff_table") as mock:
        yield mock


@pytest.fixture
def mock_roles_table():
    """Mocks the 'roles_table' database collection."""
    with patch("backend.staff.roles_table") as mock:
        yield mock


@pytest.fixture
def mock_users_table():
    """Mocks the 'users_table' database collection."""
    with patch("backend.staff.users_table") as mock:
        yield mock


@pytest.fixture
def mock_user_class():
    """Mocks the 'User' class to prevent its methods from running."""
    with patch("backend.staff.User") as mock:
        yield mock


@pytest.fixture
def mock_smtp():
    """Mocks the SMTP email client to prevent actual emails from being sent."""
    with patch("backend.staff.SMTP") as mock:
        yield mock


# This special fixture applies all the necessary mocks to every test function.
@pytest.fixture(autouse=True)
def auto_mock_dependencies(mock_staff_table, mock_roles_table, mock_users_table, mock_user_class, mock_smtp):
    """Automatically applies all mocks to every test."""
    pass


# --- Test Cases ---

def test_staff_initialization(mock_roles_table, mock_staff_table):
    """Tests that a Staff instance is initialized with correctly formatted data."""
    mock_roles_table.find_one.return_value = {"role_code": "01"}
    mock_staff_table.find_one.return_value = None  # No existing staff

    with patch('backend.staff.datetime') as mock_dt:
        mock_dt.now.return_value = datetime(2025, 1, 1)
        staff = Staff("B01", "MAN", "  john ", " DOE  ", "  John.DOE@Test.com  ", "01-01-1990", "Male", " 123 ",
                      " 123 Main St ", "Active", "01-01-2025")

        assert staff.fname == "John"
        assert staff.sname == "Doe"
        assert staff.email == "john.doe@test.com"
        assert staff.staff_email == "john.doe@baby-bargains.co.za"
        assert staff.staff_id == "STF-010125"


def test_add_staff_success(mock_staff_table, mock_roles_table, mock_user_class, mock_smtp):
    """Tests the full, successful process of adding a new staff member."""
    # Mock return values for all checks and dependencies
    mock_staff_table.count_documents.return_value = 0  # Staff does not exist
    mock_roles_table.find_one.return_value = {"role_code": "01"}
    mock_staff_table.find_one.return_value = None

    with patch('backend.staff.datetime') as mock_dt:
        mock_dt.now.return_value = datetime(2025, 1, 1)
        staff = Staff("B01", "MAN", "Jane", "Doe", "jane@test.com", "01-01-1990", "Female", "123", "addr", "Active",
                      "01-01-2025")

    # Mock the User instance that will be created
    mock_user_instance = mock_user_class.return_value
    result = staff.add_staff()

    assert result["success"] is True
    assert "created successfully" in result["message"]
    # Verify that database and User creation were called
    mock_staff_table.insert_one.assert_called_once()
    mock_user_class.assert_called_once()
    mock_user_instance.add_user.assert_called_once()
    # Verify email was sent
    assert mock_smtp.call_count == 1


def test_add_staff_already_exists(mock_staff_table):
    """Tests that adding a duplicate staff member is prevented."""
    mock_staff_table.count_documents.return_value = 1  # Staff already exists
    staff = Staff("B01", "MAN", "Jane", "Doe", "jane@test.com", "01-01-1990", "Female", "123", "addr", "Active",
                  "01-01-2025")
    result = staff.add_staff()
    assert result["success"] is False
    assert "already exists" in result["message"]


def test_delete_staff_success(mock_staff_table, mock_users_table):
    """Tests successful deletion from both staff and users tables."""
    mock_staff_table.count_documents.return_value = 1  # Staff exists
    result = Staff.delete_staff("STF-010125")
    assert result["success"] is True
    mock_staff_table.delete_one.assert_called_with({"staff_id": "STF-010125"})
    mock_users_table.delete_one.assert_called_with({"staff_id": "STF-010125"})


def test_transfer_staff_success(mock_staff_table):
    """Tests successfully transferring a staff member to a new branch."""
    mock_staff_table.count_documents.return_value = 1  # Staff exists
    result = Staff.transfer_staff("STF-010125", "B02")
    assert result["success"] is True
    mock_staff_table.update_one.assert_called_with({"staff_id": "STF-010125"}, {"$set": {"branch_code": "B02"}})


@pytest.mark.asyncio
async def test_upload_staff_csv_success(mock_staff_table, mock_roles_table, mock_user_class):
    """Tests a successful CSV upload to an empty table."""
    mock_staff_table.count_documents.return_value = 0  # Table is empty
    mock_roles_table.find_one.return_value = {"role_code": "01"}  # Role exists

    csv_content = "branch_code,role,firstname,lastname,email,date_of_birth,gender,phone_number,address,status\nB01,MAN,Test,User,test@test.com,01-01-1990,Male,123,123 St,Active"
    fake_file = AsyncMock()
    fake_file.read.return_value = csv_content.encode('utf-8')

    # Mock the datetime call inside the upload loop
    with patch('backend.staff.datetime') as mock_dt:
        mock_dt.now.return_value = datetime(2025, 1, 1)
        result = await Staff.upload_staff_csv(fake_file)

    assert result["success"] is True
    assert "successful & complete" in result["message"]


@pytest.mark.asyncio
async def test_upload_staff_csv_table_not_empty(mock_staff_table):
    """Tests that CSV upload is blocked if the table is not empty."""
    mock_staff_table.count_documents.return_value = 1  # Table has data
    fake_file = AsyncMock()
    result = await Staff.upload_staff_csv(fake_file)
    assert result["success"] is False
    assert "already contains data" in result["message"]