﻿namespace WeddingShare.Models.Database
{
    public class CustomResourceModel
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? UploadedBy { get; set; }
        public int Owner { get; set; }
    }
}