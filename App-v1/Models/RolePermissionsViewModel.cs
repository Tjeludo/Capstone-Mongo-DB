// ViewModels/RolePermissionsViewModel.cs
using App_v1.Models;
using App_v1.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace App_v1.ViewModels
{
    public class RolePermissionsViewModel
    {
        private readonly RoleApiService _apiService;
        public ObservableCollection<PermissionInfo> RolePermissions
        {
            get; set;
        }
        public string RoleName { get; private set; } = string.Empty;
        public int RoleCode
        {
            get; private set;
        }

        public RolePermissionsViewModel()
        {
            _apiService = new RoleApiService();
            RolePermissions = new ObservableCollection<PermissionInfo>();
        }

        public async Task LoadPermissionsAsync(int roleCode, string roleName)
        {
            RoleCode = roleCode;
            RoleName = roleName;
            RolePermissions.Clear();

            var response = await _apiService.GetRolePermissionsAsync(roleCode);
            if (response != null && response.Success && response.Data != null)
            {
                foreach (var permissionString in response.Data)
                {
                    RolePermissions.Add(new PermissionInfo { Name = permissionString });
                }
            }
        }
    }
}