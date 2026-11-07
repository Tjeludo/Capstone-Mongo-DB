import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from backend.roles import Role


# --- Fixtures ---

@pytest.fixture
def mock_roles_table():
    """Mocks the 'roles_table' database collection for all tests."""
    with patch("backend.roles.roles_table") as mock:
        yield mock


# This auto-use fixture applies the mock to every test function automatically.
@pytest.fixture(autouse=True)
def auto_mock_db(mock_roles_table):
    """Automatically applies the roles_table mock to every test."""
    pass


# --- Test Cases ---

def test_role_initialization(mock_roles_table):
    """Tests that a Role instance is initialized correctly."""
    mock_roles_table.find_one.return_value = None  # No existing role
    role = Role(name="Administrator", abbrev="ADM", status="active")
    assert role.role_name == "Administrator"
    assert role.role_abbrev == "ADM"
    assert role.status == "active"
    assert role.permissions == []
    assert role.role_code is not None  # Check that a code was generated


def test_add_role_success(mock_roles_table):
    """Tests the successful creation of a new role."""
    mock_roles_table.find_one.return_value = None  # Role doesn't exist
    role = Role("Manager", "MGR")
    result = role.add_role()
    assert result["success"] is True
    assert "created successfully" in result["message"]
    mock_roles_table.insert_one.assert_called_once()
    # Verify the document inserted does NOT contain 'description'
    inserted_doc = mock_roles_table.insert_one.call_args[0][0]
    assert "description" not in inserted_doc


def test_add_role_already_exists(mock_roles_table):
    """Tests that creating a duplicate role is prevented."""
    # CORRECTED: The mock document now includes the 'role_code' key
    # that the generate_role_code method expects to find.
    mock_roles_table.find_one.return_value = {"role_abbrev": "ADM", "role_code": 0}

    role = Role("Administrator", "ADM")
    result = role.add_role()

    assert result["success"] is False
    assert "already exists" in result["message"]


def test_generate_role_code_first(mock_roles_table):
    """Tests generating the first role code (0) in an empty database."""
    mock_roles_table.find_one.side_effect = [None, None]  # No existing name, no highest code
    role = Role("First Role", "FST")
    assert role.role_code == 0


def test_generate_role_code_next(mock_roles_table):
    """Tests generating a sequential role code."""
    # No existing name, but a highest code of 10 exists
    mock_roles_table.find_one.side_effect = [None, {"role_code": 10}]
    role = Role("Next Role", "NXT")
    assert role.role_code == 15


def test_update_role_success(mock_roles_table):
    """Tests a successful role update."""
    # Simulate role_exists_by_code returning True
    mock_roles_table.find_one.return_value = {"role_code": 5}
    result = Role.update_role(5, "New Name", "NEW", "inactive")

    assert result["success"] is True
    expected_update = {"$set": {"role_name": "New Name", "role_abbrev": "NEW", "status": "inactive"}}
    mock_roles_table.update_one.assert_called_with({"role_code": 5}, expected_update)


def test_delete_role_success(mock_roles_table):
    """Tests a successful role deletion."""
    mock_roles_table.find_one.return_value = {"role_code": 5}
    result = Role.delete_role(5)
    assert result["success"] is True
    mock_roles_table.delete_one.assert_called_with({"role_code": 5})


@pytest.mark.asyncio
async def test_upload_roles_csv_success():
    """Tests a successful CSV upload to an empty table."""
    # Patch the Role class inside the static method to control its behavior
    with patch("backend.roles.Role") as MockRole:
        mock_instance = MockRole.return_value
        mock_instance.add_role.return_value = {"success": True}

        # Also patch the table check
        with patch("backend.roles.roles_table") as mock_table:
            mock_table.count_documents.return_value = 0

            # UPDATED: CSV content no longer includes 'description'
            csv_content = "role_name,role_abbrev,status\nAdmin,ADM,active\nManager,MGR,inactive"
            fake_file = AsyncMock()
            fake_file.read.return_value = csv_content.encode('utf-8')

            result = await Role.upload_roles_csv(fake_file)

            assert result["success"] is True
            assert "Upload successful" in result["message"]
            assert MockRole.call_count == 2  # Called for each row


@pytest.mark.asyncio
async def test_upload_roles_csv_invalid_columns():
    """Tests CSV upload with incorrect column headers."""
    with patch("backend.roles.roles_table") as mock_table:
        mock_table.count_documents.return_value = 0

        # CSV with a missing column
        csv_content = "role_name,status\nAdmin,active"
        fake_file = AsyncMock()
        fake_file.read.return_value = csv_content.encode('utf-8')

        result = await Role.upload_roles_csv(fake_file)

        assert result["success"] is False
        assert "must contain exactly these columns" in result["message"]