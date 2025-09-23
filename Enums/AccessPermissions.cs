namespace WeddingShare.Enums
{
    [Flags]
    public enum AccessPermissions
    {
        None = 0,

        Login = 1,

        Review_View = 2,
        Review_Approve = 4,
        Review_Reject = 8,
        Review_Delete = 16,

        Gallery_View = 32,
        Gallery_Create = 64,
        Gallery_Update = 128,
        Gallery_Delete = 256,
        Gallery_Upload = 1073741824,
        Gallery_Download = 512,
        Gallery_Wipe = 1024,

        User_View = 2048,
        User_Create = 4096,
        User_Update = 8192,
        User_Delete = 16384,
        User_Change_Password = 32768,
        User_Reset_MFA = 65536,
        User_Freeze = 131072,

        CustomResource_View = 262144,
        CustomResource_Create = 524288,
        CustomResource_Update = 1048576,
        CustomResource_Delete = 2097152,

        Settings_View = 4194304,
        Settings_Update = 8388608,
        Settings_Gallery_Update = 16777216,

        Audit_View = 33554432,

        Data_View = 67108864,
        Data_Import = 134217728,
        Data_Export = 268435456,
        Data_Wipe = 536870912
    }
}