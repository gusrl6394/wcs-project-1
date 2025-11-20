using Microsoft.EntityFrameworkCore;
using Wcs.Domain;
using Wcs.Domain.Field;
using Wcs.Domain.Equipment;

namespace Wcs.Infrastructure.Persistence
{
    public class WcsDbContext : DbContext
    {
        public WcsDbContext(DbContextOptions<WcsDbContext> options)
            : base(options)
        {
        }

        // Entity sets
        public DbSet<Job> Jobs { get; set; } = default!;
        public DbSet<Command> Commands { get; set; } = default!;
        public DbSet<FieldTag> FieldTags { get; set; } = default!;
        public DbSet<EquipmentEntity> Equipments { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Job>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.PalletId).IsRequired();
                e.HasIndex(x => new { x.PalletId, x.State });
            });

            modelBuilder.Entity<Command>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.State, x.CreatedAt });
            });

            // ★ FieldTag 시드 데이터
            modelBuilder.Entity<FieldTag>().HasData(
                // 1) 컨베이어1 RUN 피드백 (Coil, 00001)
                new FieldTag
                {
                    Id = "CV01_RUN_FB",           // TagId
                    DeviceId = "PLC01",           // Modbus 디바이스/PLC 식별자
                    DataType = IoDataType.Coil,   // 0xxxx (Read Coils)
                    Direction = IoDirection.Input,// 설비 -> WCS (피드백)
                    Address = 0,                  // 0 => Coil 00001
                    BitIndex = null,              // 단일 Coil 이므로 비트 인덱스 없음
                    Description = "컨베이어1 구동 피드백",
                    EquipmentId = "CV01",         // 설비 Id (Equipment 엔티티 키)
                    PropertyName = "IsRunning"    // Equipment.IsRunning 프로퍼티와 매핑
                },

                // 2) 컨베이어1 Fault (Discrete Input, 10001)
                new FieldTag
                {
                    Id = "CV01_FAULT",
                    DeviceId = "PLC01",
                    DataType = IoDataType.DiscreteInput,   // 1xxxx (Read Discrete Inputs)
                    Direction = IoDirection.Input,
                    Address = 0,                           // 0 => DI 10001
                    BitIndex = null,
                    Description = "컨베이어1 Fault 상태",
                    EquipmentId = "CV01",
                    PropertyName = "HasFault"              // Equipment.HasFault
                },

                // 3) 컨베이어1 입구 포토센서 (Discrete Input, 10002)
                new FieldTag
                {
                    Id = "CV01_PE_IN",
                    DeviceId = "PLC01",
                    DataType = IoDataType.DiscreteInput,
                    Direction = IoDirection.Input,
                    Address = 1,                           // 1 => DI 10002
                    BitIndex = null,
                    Description = "컨베이어1 입구 포토센서",
                    EquipmentId = "CV01",
                    PropertyName = "IsBlocked"            // Equipment.IsBlocked
                },

                        new FieldTag
                {
                    Id = "CV02_SPEED",
                    DeviceId = "PLC02",
                    DataType = IoDataType.HoldingRegister,
                    Direction = IoDirection.Input,
                    Address = 0,             // 40001
                    BitIndex = null,
                    Description = "CV02 속도 (rpm)",
                    EquipmentId = "CV02",
                    PropertyName = "SpeedRpm"
                },
                new FieldTag
                {
                    Id = "CV02_TEMP",
                    DeviceId = "PLC02",
                    DataType = IoDataType.InputRegister,
                    Direction = IoDirection.Input,
                    Address = 0,             // 30001
                    BitIndex = null,
                    Description = "CV02 온도 (Raw 값)",
                    EquipmentId = "CV02",
                    PropertyName = "TemperatureRaw"
                }
            );
        }
    }
}
