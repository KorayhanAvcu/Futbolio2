using Futbolio.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Futbolio.API.Context
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options)
            : base(options)
        {
        }

        // Tables

        public DbSet<Team> Teams { get; set; }

        public DbSet<Match> Matches { get; set; }

        public DbSet<Match_Goal> MatchGoals { get; set; }

        public DbSet<Match_Card> MatchCards { get; set; }

        public DbSet<Substitution> Substitutions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Match -> HomeTeam

            modelBuilder.Entity<Match>()
                .HasOne(x => x.HomeTeam)
                .WithMany(x => x.HomeMatches)
                .HasForeignKey(x => x.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);


            // Match -> AwayTeam

            modelBuilder.Entity<Match>()
                .HasOne(x => x.AwayTeam)
                .WithMany(x => x.AwayMatches)
                .HasForeignKey(x => x.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);


            // Match_Goal -> Match

            modelBuilder.Entity<Match_Goal>()
                .HasOne(x => x.Match)
                .WithMany(x => x.Goals)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);


            // Match_Goal -> Team

            modelBuilder.Entity<Match_Goal>()
                .HasOne(x => x.Team)
                .WithMany(x => x.Goals)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);


            // Match_Card -> Match

            modelBuilder.Entity<Match_Card>()
                .HasOne(x => x.Match)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);


            // Match_Card -> Team

            modelBuilder.Entity<Match_Card>()
                .HasOne(x => x.Team)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);


            // Substitution -> Match

            modelBuilder.Entity<Substitution>()
                .HasOne(x => x.Match)
                .WithMany(x => x.Substitutions)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);


            // Substitution -> Team

            modelBuilder.Entity<Substitution>()
                .HasOne(x => x.Team)
                .WithMany(x => x.Substitutions)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}