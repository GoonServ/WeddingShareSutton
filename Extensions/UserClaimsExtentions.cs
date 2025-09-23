using System.Security.Claims;
using System.Security.Principal;
using WeddingShare.Enums;

namespace WeddingShare.Extensions
{
    public static class UserClaimsExtentions
    {
        public static int GetUserId(this IIdentity identity)
        {
            try
            {
                return int.Parse(((ClaimsIdentity)identity).Claims.FirstOrDefault(x => string.Equals(ClaimTypes.Sid, x.Type, StringComparison.OrdinalIgnoreCase))?.Value ?? "-1");
            }
            catch { }

            return -1;
        }

        public static UserLevel GetUserLevel(this IIdentity identity)
        {
            try
            {
                var level = ((ClaimsIdentity)identity).Claims.FirstOrDefault(x => string.Equals(ClaimTypes.Role, x.Type, StringComparison.OrdinalIgnoreCase))?.Value;
                foreach (UserLevel l in Enum.GetValues(typeof(UserLevel)))
                {
                    if (l.ToString().Equals(level, StringComparison.OrdinalIgnoreCase))
                    {
                        return l;
                    }
                }
            }
            catch { }

            return UserLevel.Basic;
        }

        public static AccessPermissions GetUserPermissions(this IIdentity identity)
        {
            try
            {
                var level = identity.GetUserLevel();
                switch (level)
                {
                    case UserLevel.Basic:
                        return AccessPermissions.Login;
                    case UserLevel.Reviewer:
                        return
                            AccessPermissions.Login
                            | AccessPermissions.Review_View
                            | AccessPermissions.Review_Approve
                            | AccessPermissions.Review_Reject
                            | AccessPermissions.Review_Delete
                            | AccessPermissions.Gallery_View;
                    case UserLevel.Moderator:
                        return
                            AccessPermissions.Login
                            | AccessPermissions.Review_View
                            | AccessPermissions.Review_Approve
                            | AccessPermissions.Review_Reject
                            | AccessPermissions.Review_Delete
                            | AccessPermissions.Gallery_View
                            | AccessPermissions.Gallery_Update
                            | AccessPermissions.Gallery_Upload
                            | AccessPermissions.Gallery_Download
                            | AccessPermissions.User_View
                            | AccessPermissions.User_Reset_MFA
                            | AccessPermissions.User_Freeze
                            | AccessPermissions.CustomResource_View
                            | AccessPermissions.Audit_View;
                    case UserLevel.Admin:
                        return
                            AccessPermissions.Login
                            | AccessPermissions.Review_View
                            | AccessPermissions.Review_Approve
                            | AccessPermissions.Review_Reject
                            | AccessPermissions.Review_Delete
                            | AccessPermissions.Gallery_View
                            | AccessPermissions.Gallery_Create
                            | AccessPermissions.Gallery_Update
                            | AccessPermissions.Gallery_Delete
                            | AccessPermissions.Gallery_Upload
                            | AccessPermissions.Gallery_Download
                            | AccessPermissions.User_View
                            | AccessPermissions.User_Create
                            | AccessPermissions.User_Update
                            | AccessPermissions.User_Change_Password
                            | AccessPermissions.User_Reset_MFA
                            | AccessPermissions.User_Freeze
                            | AccessPermissions.CustomResource_View
                            | AccessPermissions.CustomResource_Create
                            | AccessPermissions.CustomResource_Update
                            | AccessPermissions.CustomResource_Delete
                            | AccessPermissions.Settings_View
                            | AccessPermissions.Settings_Update
                            | AccessPermissions.Settings_Gallery_Update
                            | AccessPermissions.Audit_View;
                    case UserLevel.Owner:
                        return
                            AccessPermissions.Login
                            | AccessPermissions.Review_View
                            | AccessPermissions.Review_Approve
                            | AccessPermissions.Review_Reject
                            | AccessPermissions.Review_Delete
                            | AccessPermissions.Gallery_View
                            | AccessPermissions.Gallery_Create
                            | AccessPermissions.Gallery_Update
                            | AccessPermissions.Gallery_Delete
                            | AccessPermissions.Gallery_Upload
                            | AccessPermissions.Gallery_Download
                            | AccessPermissions.Gallery_Wipe
                            | AccessPermissions.User_View
                            | AccessPermissions.User_Create
                            | AccessPermissions.User_Update
                            | AccessPermissions.User_Delete
                            | AccessPermissions.User_Change_Password
                            | AccessPermissions.User_Reset_MFA
                            | AccessPermissions.User_Freeze
                            | AccessPermissions.CustomResource_View
                            | AccessPermissions.CustomResource_Create
                            | AccessPermissions.CustomResource_Update
                            | AccessPermissions.CustomResource_Delete
                            | AccessPermissions.Settings_View
                            | AccessPermissions.Settings_Update
                            | AccessPermissions.Settings_Gallery_Update
                            | AccessPermissions.Audit_View
                            | AccessPermissions.Data_View
                            | AccessPermissions.Data_Import
                            | AccessPermissions.Data_Export
                            | AccessPermissions.Data_Wipe;
                    default: 
                        return AccessPermissions.None;
                }

            }
            catch { }

            return AccessPermissions.None;
        }
    }
}