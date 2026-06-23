    using System;
    using System.Collections.Generic;
    using AdopcionDbAPI.Models;
    using Microsoft.EntityFrameworkCore;

    namespace AdopcionDbAPI.Context;

    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Adopter> Adopters { get; set; }

    public virtual DbSet<Recommendation> Recommendations { get; set; }

    public virtual DbSet<AdoptionRequest> AdoptionRequests { get; set; }

        public virtual DbSet<AuditLog> AuditLogs { get; set; }

        public virtual DbSet<Breed> Breeds { get; set; }

        public virtual DbSet<Pet> Pets { get; set; }

        public virtual DbSet<PetImage> PetImages { get; set; }

        public virtual DbSet<PetStatus> PetStatuses { get; set; }

        public virtual DbSet<RequestStatus> RequestStatuses { get; set; }

        public virtual DbSet<Review> Reviews { get; set; }

        public virtual DbSet<Role> Roles { get; set; }

        public virtual DbSet<Size> Sizes { get; set; }

        public virtual DbSet<Species> Species { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<vw_AdoptersProfile> vw_AdoptersProfiles { get; set; }

        public virtual DbSet<vw_AdoptionRequestDetail> vw_AdoptionRequestDetails { get; set; }

        public virtual DbSet<vw_AvailablePet> vw_AvailablePets { get; set; }

        public virtual DbSet<vw_ExecutiveAdoptionReport> vw_ExecutiveAdoptionReports { get; set; }

        public virtual DbSet<vw_PetReviewSummary> vw_PetReviewSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Adopter>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Adopters__3213E83F2B710420");

                entity.Property(e => e.createdAt).HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.user).WithOne(p => p.Adopter)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Adopters_Users");
            });

        modelBuilder.Entity<AdoptionRequest>(entity =>
        {
            entity.HasKey(e => e.id)
                .HasName("PK__Adoption__3213E83F9E71C7F3");

            entity.ToTable(tb =>
            {
                tb.HasTrigger("trg_AdoptionRequests_Audit_Delete");
                tb.HasTrigger("trg_AdoptionRequests_AutoPetStatus");
            });

            entity.Property(e => e.createdAt)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.Property(e => e.statusId)
            .HasDefaultValue(1);

            entity.Property(e => e.decisionNotes)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.reviewedByUserId, "IX_AdoptionRequests_ReviewedByUserId");

            entity.HasOne(d => d.adopter)
                .WithMany(p => p.AdoptionRequests)
                .HasForeignKey(d => d.adopterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdoptionRequests_Adopters");

            entity.HasOne(d => d.pet)
                .WithMany(p => p.AdoptionRequests)
                .HasForeignKey(d => d.petId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdoptionRequests_Pets");

            entity.HasOne(d => d.status)
                .WithMany(p => p.AdoptionRequests)
                .HasForeignKey(d => d.statusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdoptionRequests_RequestStatuses");

            entity.HasOne(d => d.reviewedByUser)
                .WithMany(p => p.ReviewedAdoptionRequests)
                .HasForeignKey(d => d.reviewedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdoptionRequests_ReviewedByUser");

            entity.HasIndex(e => new { e.adopterId, e.petId }, "UX_AdoptionRequests_AdopterPet_Pending")
                .IsUnique()
                .HasFilter("([statusId]=(1))");

            entity.HasIndex(e => e.petId, "UX_AdoptionRequests_OneApprovedPerPet")
                .IsUnique()
                .HasFilter("([statusId]=(2))");
        });

        modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(e => e.actionDate).HasDefaultValueSql("(sysdatetime())", "DF_AuditLogs_ActionDate");
                entity.Property(e => e.userName).HasDefaultValueSql("(suser_sname())", "DF_AuditLogs_UserName");
            });

            modelBuilder.Entity<Breed>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Breeds__3213E83F1F23554A");

                entity.HasOne(d => d.species).WithMany(p => p.Breeds)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Breeds_Species");
            });

            modelBuilder.Entity<Pet>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Pets__3213E83FD4D078A0");

                entity.ToTable(tb => tb.HasTrigger("trg_Pets_Audit_Insert"));

                entity.Property(e => e.createdAt).HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.statusId).HasDefaultValue(1);

                entity.HasOne(d => d.breed).WithMany(p => p.Pets).HasConstraintName("FK_Pets_Breeds");

                entity.HasOne(d => d.size).WithMany(p => p.Pets)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Pets_Sizes");

                entity.HasOne(d => d.species).WithMany(p => p.Pets)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Pets_Species");

                entity.HasOne(d => d.status).WithMany(p => p.Pets)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Pets_PetStatuses");

                entity.Property(e => e.isVaccinated)
                    .HasDefaultValue(false);

                entity.Property(e => e.isSterilized)
                    .HasDefaultValue(false);

                entity.Property(e => e.isDewormed)
                    .HasDefaultValue(false);

                entity.Property(e => e.medicalNotes)
                    .HasMaxLength(1000);
            });

             modelBuilder.Entity<PetImage>(entity =>
                {
                    entity.HasKey(e => e.id)
                        .HasName("PK__PetImage__3213E83F3A04A786");

                    entity.HasIndex(e => e.petId, "UX_PetImages_OnePrimaryPerPet")
                        .IsUnique()
                        .HasFilter("([isPrimary]=(1))");

                    entity.Property(e => e.createdAt)
                        .HasDefaultValueSql("(sysutcdatetime())");

                    entity.HasOne(d => d.pet)
                        .WithMany(p => p.PetImages)
                        .HasForeignKey(d => d.petId)
                        .HasConstraintName("FK_PetImages_Pets");
                });

        modelBuilder.Entity<PetStatus>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__PetStatu__3213E83F784C9F3F");
            });

            modelBuilder.Entity<RequestStatus>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__RequestS__3213E83F8EE9D044");
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Reviews__3213E83F654BB929");

                entity.Property(e => e.createdAt).HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.pet).WithMany(p => p.Reviews)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Reviews_Pets");

                entity.HasOne(d => d.user).WithMany(p => p.Reviews)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Reviews_Users");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Roles__3213E83F2B909C37");
            });

            modelBuilder.Entity<Size>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Sizes__3213E83F73A1E2B7");
            });

            modelBuilder.Entity<Species>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Species__3213E83F8EA5E937");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.id).HasName("PK__Users__3213E83FE4D2ECFA");

                entity.Property(e => e.createdAt).HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.roleId).HasDefaultValue(2);

                entity.HasOne(d => d.role).WithMany(p => p.Users)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Users_Roles");
            });

            modelBuilder.Entity<vw_AdoptersProfile>(entity =>
            {
                entity.ToView("vw_AdoptersProfile");
            });

            modelBuilder.Entity<vw_AdoptionRequestDetail>(entity =>
            {
                entity.ToView("vw_AdoptionRequestDetails");
            });

            modelBuilder.Entity<vw_AvailablePet>(entity =>
            {
                entity.ToView("vw_AvailablePets");
            });

            modelBuilder.Entity<vw_ExecutiveAdoptionReport>(entity =>
            {
                entity.ToView("vw_ExecutiveAdoptionReport");
            });

            modelBuilder.Entity<vw_PetReviewSummary>(entity =>
            {
                entity.ToView("vw_PetReviewSummary");
            });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK_Recommendations");

            entity.Property(e => e.name).HasMaxLength(100);
            entity.Property(e => e.email).HasMaxLength(150);
            entity.Property(e => e.status).HasMaxLength(30).HasDefaultValue("Pendiente");
            entity.Property(e => e.adminNotes).HasMaxLength(1000);
            entity.Property(e => e.createdAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.user)
                .WithMany()
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Recommendations_Users");
        });

        OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
