namespace WeddingShare.Helpers
{
    public static class ErrorCode
    {
        public const int UnexpectedError = 400;
        public const int Unauthorized = 401;
        public const int InvalidSecretKey = 402;
        public const int GalleryCreationNotAllowed = 403;
        public const int GalleryLimitReached = 405;
        public const int InvalidGalleryId = 406;
    }
}