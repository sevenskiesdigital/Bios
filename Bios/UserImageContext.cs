﻿using Microsoft.EntityFrameworkCore;

namespace Bios
{
    public class UserImageContext : DbContext 
    {
        public UserImageContext(DbContextOptions<UserImageContext> options):base(options)
        {

        }

        public DbSet<UserImage> UserImages { get; set; }
    }

    public class UserImage
    {
        public int Id { get; set; }
        public string? FaceId { get; set; }

        public int UserId { get; set; }

        public string? ImageUrl { get; set; }

        public int? DeleteStatus { get; set; }

        public int? SubmitStatus { get; set; }

        public DateTime? SubmitTime { get; set; }

        public DateTime? CreatedTime { get; set; }

        public DateTime? UpdatedTime { get; set; }
    }
}
