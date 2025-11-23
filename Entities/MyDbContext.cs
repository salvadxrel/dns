using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using dns.Entities;

namespace dns.DB;

public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Category { get; set; }

    public virtual DbSet<Comment> Comment { get; set; }

    public virtual DbSet<Order> Order { get; set; }

    public virtual DbSet<OrderProduct> OrderProduct { get; set; }

    public virtual DbSet<Product> Product { get; set; }

    public virtual DbSet<Role> Role { get; set; }

    public virtual DbSet<StatusOrder> StatusOrder { get; set; }

    public virtual DbSet<User> User { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=root");
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("Category_pkey");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(40)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("Comment_pkey");

            entity.Property(e => e.CommentId).HasColumnName("comment_id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UsersId).HasColumnName("users_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Comment)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_product_id_comment");

            entity.HasOne(d => d.Users).WithMany(p => p.Comment)
                .HasForeignKey(d => d.UsersId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_id_comment");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("Order_pkey");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Address)
                .HasMaxLength(150)
                .HasColumnName("address");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderStatusId).HasColumnName("order_status_id");
            entity.Property(e => e.UsersId).HasColumnName("users_id");

            entity.HasOne(d => d.OrderStatus).WithMany(p => p.Order)
                .HasForeignKey(d => d.OrderStatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_order_status_id");

            entity.HasOne(d => d.Users).WithMany(p => p.Order)
                .HasForeignKey(d => d.UsersId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_id");
        });

        modelBuilder.Entity<OrderProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Order_product_pkey");

            entity.ToTable("Order_product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderProduct)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_order_id");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderProduct)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_product_id_op");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("Product_pkey");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10)
                .HasColumnName("price");

            entity.HasOne(d => d.Category).WithMany(p => p.Product)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_category_id");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Role_pkey");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Discription).HasColumnName("discription");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<StatusOrder>(entity =>
        {
            entity.HasKey(e => e.StatusOrderId).HasName("Status_order_pkey");

            entity.ToTable("Status_order");

            entity.Property(e => e.StatusOrderId).HasColumnName("status_order_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UsersId).HasName("User_pkey");

            entity.HasIndex(e => e.Login, "User_login_key").IsUnique();

            entity.Property(e => e.UsersId).HasColumnName("users_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("created_at");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.Mail)
                .HasMaxLength(200)
                .HasColumnName("mail");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(11)
                .HasColumnName("phone");
            entity.Property(e => e.Address)
                .HasColumnName("address");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.User)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_role_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
