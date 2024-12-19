using Microsoft.EntityFrameworkCore;
using MonRestoAPI.Models;

namespace MonRestoAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets pour vos entités
        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; } // Ajout du DbSet pour Payment

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurations pour Article
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Category)        // Relation Article -> Category
                .WithMany()                     // Une Category peut avoir plusieurs Articles
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Interdit la suppression en cascade

            // Configurations pour User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique(); // Empêche les doublons de noms d'utilisateur

            // Configurations pour OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(o => o.Article)
                .WithMany()
                .HasForeignKey(o => o.ArticleId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(o => o.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(o => o.OrderId);

            // Configurations pour Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)            // Relation Payment -> User
                .WithMany()                     // Un User peut avoir plusieurs Payments (One-to-Many)
                .HasForeignKey(p => p.UserId)   // Clé étrangère sur UserId
                .OnDelete(DeleteBehavior.Cascade); // Permet la suppression en cascade
        }
    }
}
