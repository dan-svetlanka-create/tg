using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TelegramBot_Dan.Classes
{
    public class DbConfig : DbContext
    {
        public DbSet<Command> CommandUser { get; set; }
        public DbSet<Users> Users { get; set; }  // <-- Добавьте эту строку
        public DbSet<Events> Events { get; set; } // <-- Добавьте эту строку
        public DbSet<RecurrencePattern> RecurrencePattern { get; set; } // <-- Добавьте эту строку

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql("server=127.0.0.1;port=3307;user=root;password=;database=TgBot;",
                new MySqlServerVersion(new Version(8, 0)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Конфигурация для Users
            modelBuilder.Entity<Users>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<Users>()
                .HasMany(u => u.Events)
                .WithOne()
                .HasForeignKey("UserId") // Указываем внешний ключ
                .OnDelete(DeleteBehavior.Cascade); // Добавлено каскадное удаление

            modelBuilder.Entity<Users>()
                .HasMany(u => u.RecurrencePattern)
                .WithOne()
                .HasForeignKey("UserId") // Указываем внешний ключ
                .OnDelete(DeleteBehavior.Cascade); // Добавлено каскадное удаление

            // Конфигурация для Events - добавляем свойство для внешнего ключа
            modelBuilder.Entity<Events>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Events>()
                .Property<int>("UserId"); // <-- ДОБАВЬТЕ ЭТУ СТРОКУ для внешнего ключа

            // Конфигурация для RecurrencePattern - добавляем свойство для внешнего ключа
            modelBuilder.Entity<RecurrencePattern>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<RecurrencePattern>()
                .Property<int>("UserId"); // <-- ДОБАВЬТЕ ЭТУ СТРОКУ для внешнего ключа

            // Конфигурация для Command
            modelBuilder.Entity<Command>()
                .HasKey(c => c.Id);
        }
    }
}
