import pytest
from unittest.mock import patch
from backend.permissions import Permission

@pytest.fixture
def mock_permissions_table():
    with patch("backend.permissions.permissions_table") as mock:
        yield mock

@pytest.fixture
def mock_roles_table():
    with patch("backend.permissions.roles_table") as mock:
        yield mock

@pytest.fixture(autouse=True)
def auto_mock_db(mock_permissions_table, mock_roles_table):
    pass

def test_add_permission_success(mock_permissions_table):
    """Tests successful creation of a new permission."""
    mock_permissions_table.find_one.return_value = None # Permission doesn't exist
    perm = Permission("view_users", "active")
    result = perm.add_permission()
    assert result["success"] is True
    mock_permissions_table.insert_one.assert_called_once()
    # Check that the inserted document does not have a description
    inserted_doc = mock_permissions_table.insert_one.call_args[0][0]
    assert "perm_description" not in inserted_doc

def test_update_permission_success(mock_permissions_table):
    """Tests a successful permission update."""
    mock_permissions_table.find_one.return_value = {"perm_id": "PERM-001"} # Permission exists
    result = Permission.update_permission("PERM-001", "edit_users", "inactive")
    assert result["success"] is True
    expected_update = {"$set": {"perm_name": "edit_users", "perm_status": "inactive"}}
    mock_permissions_table.update_one.assert_called_with({"perm_id": "PERM-001"}, expected_update)

def test_grant_permission_success(mock_permissions_table, mock_roles_table):
    """Tests successfully granting a permission to a role."""
    mock_permissions_table.find_one.return_value = {"perm_id": "PERM-001", "perm_name": "view_users"}
    # Role exists but does not have the permission yet
    mock_roles_table.find_one.return_value = {"role_name": "Manager", "permissions": []}
    result = Permission.grant_permission("PERM-001", "Manager")
    assert result["success"] is True
    mock_roles_table.update_one.assert_called_with({"role_name": "Manager"}, {"$addToSet": {"permissions": "view_users"}})

def test_revoke_permission_success(mock_permissions_table, mock_roles_table):
    """Tests successfully revoking a permission from a role."""
    mock_permissions_table.find_one.return_value = {"perm_id": "PERM-001", "perm_name": "view_users"}
    # Role exists and has the permission
    mock_roles_table.find_one.return_value = {"role_name": "Manager", "permissions": ["view_users"]}
    result = Permission.revoke_permission("PERM-001", "Manager")
    assert result["success"] is True
    mock_roles_table.update_one.assert_called_with({"role_name": "Manager"}, {"$pull": {"permissions": "view_users"}})