using System;
using System.Runtime.InteropServices;
using System.Text;
using Car.Example.V0;
using Car.Example.V0.Runtime;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests based on the canonical Car example from real-logic/simple-binary-encoding.
    /// Validates: nested groups, group-level data, multiple data fields, composites,
    /// enums, sets, fixed-length arrays, and constant fields.
    /// </summary>
    public class CarExampleIntegrationTests
    {
        [Fact]
        public void CarSchema_GeneratesExpectedTypes()
        {
            Assert.NotNull(typeof(CarData));
            Assert.NotNull(typeof(Engine));
            Assert.NotNull(typeof(BooleanType));
            Assert.NotNull(typeof(Model));
            Assert.NotNull(typeof(OptionalExtras));
            Assert.NotNull(typeof(ModelYear));
            Assert.NotNull(typeof(VehicleCode));
            Assert.NotNull(typeof(Ron));
            Assert.NotNull(typeof(CarData.FuelFiguresData));
            Assert.NotNull(typeof(CarData.PerformanceFiguresData));
            Assert.NotNull(typeof(CarData.AccelerationData));
            Assert.NotNull(typeof(GroupSizeEncoding));
            Assert.NotNull(typeof(VarStringEncoding));
            Assert.NotNull(typeof(VarAsciiEncoding));
        }

        [Fact]
        public void CarData_HasCorrectConstants()
        {
            Assert.Equal(1, CarData.MESSAGE_ID);
            Assert.True(CarData.MESSAGE_SIZE > 0);
        }

        [Fact]
        public unsafe void CarData_MessageSize_MatchesRuntimeSize()
        {
            Assert.Equal(CarData.MESSAGE_SIZE, sizeof(CarData));
        }

        [Fact]
        public unsafe void CarData_FixedFieldsRoundTrip()
        {
            Span<byte> buffer = stackalloc byte[CarData.MESSAGE_SIZE];
            ref CarData car = ref MemoryMarshal.AsRef<CarData>(buffer);

            car.SerialNumber = 1234567890UL;
            car.ModelYear = 2024;
            car.Available = BooleanType.T;
            car.Code = Model.B;
            car.Extras = OptionalExtras.SunRoof | OptionalExtras.CruiseControl;

            car.Engine.Capacity = 2000;
            car.Engine.NumCylinders = 4;

            Assert.Equal(1234567890UL, car.SerialNumber);
            Assert.Equal((ushort)2024, car.ModelYear.Value);
            Assert.Equal(BooleanType.T, car.Available);
            Assert.Equal(Model.B, car.Code);
            Assert.True((car.Extras & OptionalExtras.SunRoof) != 0);
            Assert.True((car.Extras & OptionalExtras.CruiseControl) != 0);
            Assert.True((car.Extras & OptionalExtras.SportsPack) == 0);
            Assert.Equal((ushort)2000, car.Engine.Capacity);
            Assert.Equal((byte)4, car.Engine.NumCylinders);
        }

        [Fact]
        public void Engine_ConstantMaxRpm_IsAccessible()
        {
            Assert.Equal((ushort)9000, Engine.MaxRpm);
        }

        [Fact]
        public unsafe void CarData_TryParse_Works()
        {
            Span<byte> buffer = stackalloc byte[CarData.MESSAGE_SIZE];
            ref CarData car = ref MemoryMarshal.AsRef<CarData>(buffer);
            car.SerialNumber = 42;
            car.Engine.Capacity = 1600;

            var success = CarData.TryParse(buffer, out var parsed, out var remaining);

            Assert.True(success);
            Assert.Equal(42UL, parsed.SerialNumber);
            Assert.Equal((ushort)1600, parsed.Engine.Capacity);
        }

        [Fact]
        public unsafe void CarData_TryEncode_Works()
        {
            CarData car = default;
            car.SerialNumber = 99;
            car.ModelYear = 2025;
            car.Available = BooleanType.F;
            car.Code = Model.A;

            Span<byte> buffer = stackalloc byte[CarData.MESSAGE_SIZE];
            Assert.True(car.TryEncode(buffer, out int bytesWritten));
            Assert.Equal(CarData.MESSAGE_SIZE, bytesWritten);

            var readBack = MemoryMarshal.AsRef<CarData>(buffer);
            Assert.Equal(99UL, readBack.SerialNumber);
            Assert.Equal((ushort)2025, readBack.ModelYear.Value);
        }

        [Fact]
        public void CarData_WriteHeader_PopulatesCorrectly()
        {
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE + CarData.MESSAGE_SIZE];

            var headerSize = CarData.WriteHeader(buffer);

            ref var header = ref MemoryMarshal.AsRef<MessageHeader>(buffer);
            Assert.Equal(MessageHeader.MESSAGE_SIZE, headerSize);
            Assert.Equal((ushort)CarData.BLOCK_LENGTH, header.BlockLength);
            Assert.Equal((ushort)CarData.MESSAGE_ID, header.TemplateId);
            Assert.Equal((ushort)10, header.SchemaId);
            Assert.Equal((ushort)0, header.Version);
        }

        [Fact]
        public void CarData_ToString_ContainsFieldNames()
        {
            CarData car = default;
            car.SerialNumber = 12345;

            string result = car.ToString();

            Assert.Contains("CarData", result);
            Assert.Contains("SerialNumber", result);
            Assert.Contains("12345", result);
        }

        [Fact]
        public void FuelFigures_GroupStructLayout()
        {
            Assert.True(CarData.FuelFiguresData.MESSAGE_SIZE > 0);

            Span<byte> buffer = stackalloc byte[CarData.FuelFiguresData.MESSAGE_SIZE];
            ref CarData.FuelFiguresData fuel = ref MemoryMarshal.AsRef<CarData.FuelFiguresData>(buffer);
            fuel.Speed = 60;
            fuel.Mpg = 30.5f;

            Assert.Equal((ushort)60, fuel.Speed);
            Assert.Equal(30.5f, fuel.Mpg);
        }

        [Fact]
        public void PerformanceFigures_NestedGroupStructLayout()
        {
            Assert.True(CarData.PerformanceFiguresData.MESSAGE_SIZE > 0);
            Assert.True(CarData.AccelerationData.MESSAGE_SIZE > 0);

            Span<byte> buffer = stackalloc byte[CarData.AccelerationData.MESSAGE_SIZE];
            ref CarData.AccelerationData accel = ref MemoryMarshal.AsRef<CarData.AccelerationData>(buffer);
            accel.Mph = 60;
            accel.Seconds = 6.2f;

            Assert.Equal((ushort)60, accel.Mph);
            Assert.Equal(6.2f, accel.Seconds);
        }

        [Fact]
        public void VarAsciiEncoding_CreateAndTotalLength()
        {
            byte[] data = new byte[4 + 5]; // "Hello" = 5 bytes
            BitConverter.TryWriteBytes(data.AsSpan(0, 4), (uint)5);
            Encoding.ASCII.GetBytes("Hello", data.AsSpan(4));

            var varAscii = VarAsciiEncoding.Create(data);

            Assert.Equal(5u, varAscii.Length);
            Assert.Equal(5, varAscii.VarData.Length);
            Assert.Equal(9, varAscii.TotalLength); // 4 + 5
            Assert.Equal("Hello", Encoding.ASCII.GetString(varAscii.VarData));
        }

        [Fact]
        public void VarStringEncoding_CreateAndTotalLength()
        {
            byte[] data = new byte[4 + 3]; // "SBE" = 3 bytes
            BitConverter.TryWriteBytes(data.AsSpan(0, 4), (uint)3);
            Encoding.UTF8.GetBytes("SBE", data.AsSpan(4));

            var varString = VarStringEncoding.Create(data);

            Assert.Equal(3u, varString.Length);
            Assert.Equal(3, varString.VarData.Length);
            Assert.Equal(7, varString.TotalLength); // 4 + 3
        }

        [Fact]
        public unsafe void CarData_ConsumeVariableLengthSegments_WithGroupData()
        {
            var buffer = new byte[2048];
            var span = buffer.AsSpan();
            int offset = 0;

            // Write Car fixed fields
            ref CarData car = ref MemoryMarshal.AsRef<CarData>(span);
            car.SerialNumber = 42;
            car.Engine.Capacity = 2000;
            offset += CarData.MESSAGE_SIZE;

            // fuelFigures group: 1 entry
            ref GroupSizeEncoding fuelGroupHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(span.Slice(offset));
            fuelGroupHeader.BlockLength = (ushort)CarData.FuelFiguresData.MESSAGE_SIZE;
            fuelGroupHeader.NumInGroup = 1;
            offset += GroupSizeEncoding.MESSAGE_SIZE;

            ref CarData.FuelFiguresData fuelEntry = ref MemoryMarshal.AsRef<CarData.FuelFiguresData>(span.Slice(offset));
            fuelEntry.Speed = 60;
            fuelEntry.Mpg = 30.5f;
            offset += CarData.FuelFiguresData.MESSAGE_SIZE;

            // usageDescription varData for fuelFigures entry
            var usageBytes = Encoding.ASCII.GetBytes("City");
            BitConverter.TryWriteBytes(span.Slice(offset, 4), (uint)usageBytes.Length);
            usageBytes.CopyTo(span.Slice(offset + 4));
            offset += 4 + usageBytes.Length;

            // performanceFigures group: 1 entry
            ref GroupSizeEncoding perfGroupHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(span.Slice(offset));
            perfGroupHeader.BlockLength = (ushort)CarData.PerformanceFiguresData.MESSAGE_SIZE;
            perfGroupHeader.NumInGroup = 1;
            offset += GroupSizeEncoding.MESSAGE_SIZE;

            ref CarData.PerformanceFiguresData perfEntry = ref MemoryMarshal.AsRef<CarData.PerformanceFiguresData>(span.Slice(offset));
            perfEntry.OctaneRating = 95;
            offset += CarData.PerformanceFiguresData.MESSAGE_SIZE;

            // acceleration nested group: 2 entries
            ref GroupSizeEncoding accelGroupHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(span.Slice(offset));
            accelGroupHeader.BlockLength = (ushort)CarData.AccelerationData.MESSAGE_SIZE;
            accelGroupHeader.NumInGroup = 2;
            offset += GroupSizeEncoding.MESSAGE_SIZE;

            ref CarData.AccelerationData accel1 = ref MemoryMarshal.AsRef<CarData.AccelerationData>(span.Slice(offset));
            accel1.Mph = 30;
            accel1.Seconds = 3.8f;
            offset += CarData.AccelerationData.MESSAGE_SIZE;

            ref CarData.AccelerationData accel2 = ref MemoryMarshal.AsRef<CarData.AccelerationData>(span.Slice(offset));
            accel2.Mph = 60;
            accel2.Seconds = 7.5f;
            offset += CarData.AccelerationData.MESSAGE_SIZE;

            // manufacturer varData
            var mfgBytes = Encoding.UTF8.GetBytes("Honda");
            BitConverter.TryWriteBytes(span.Slice(offset, 4), (uint)mfgBytes.Length);
            mfgBytes.CopyTo(span.Slice(offset + 4));
            offset += 4 + mfgBytes.Length;

            // model varData
            var modelBytes = Encoding.UTF8.GetBytes("Civic");
            BitConverter.TryWriteBytes(span.Slice(offset, 4), (uint)modelBytes.Length);
            modelBytes.CopyTo(span.Slice(offset + 4));
            offset += 4 + modelBytes.Length;

            // activationCode varData
            var codeBytes = Encoding.ASCII.GetBytes("ABC");
            BitConverter.TryWriteBytes(span.Slice(offset, 4), (uint)codeBytes.Length);
            codeBytes.CopyTo(span.Slice(offset + 4));
            offset += 4 + codeBytes.Length;

            // Parse: directly slice past the fixed fields since the struct size
            // includes padding from embedded composites
            var parsedCar = MemoryMarshal.AsRef<CarData>(span);
            var variableData = span.Slice(CarData.MESSAGE_SIZE);
            Assert.Equal(42UL, parsedCar.SerialNumber);

            int fuelCount = 0;
            int perfCount = 0;
            int accelCount = 0;
            string? manufacturer = null;
            string? modelName = null;

            parsedCar.ConsumeVariableLengthSegments(variableData,
                callbackFuelFigures: (in CarData.FuelFiguresData fuel) =>
                {
                    fuelCount++;
                    Assert.Equal((ushort)60, fuel.Speed);
                    Assert.Equal(30.5f, fuel.Mpg);
                },
                callbackFuelFiguresUsageDescription: (VarAsciiEncoding usageDesc) =>
                {
                    Assert.Equal("City", Encoding.ASCII.GetString(usageDesc.VarData));
                },
                callbackPerformanceFigures: (in CarData.PerformanceFiguresData perf) =>
                {
                    perfCount++;
                    Assert.Equal((byte)95, perf.OctaneRating.Value);
                },
                callbackAcceleration: (in CarData.PerformanceFiguresData perf, in CarData.AccelerationData accel) =>
                {
                    accelCount++;
                },
                callbackManufacturer: (VarStringEncoding mfg) =>
                {
                    manufacturer = Encoding.UTF8.GetString(mfg.VarData);
                },
                callbackModel: (VarStringEncoding mdl) =>
                {
                    modelName = Encoding.UTF8.GetString(mdl.VarData);
                },
                callbackActivationCode: (VarAsciiEncoding code) =>
                {
                    Assert.Equal("ABC", Encoding.ASCII.GetString(code.VarData));
                }
            );

            Assert.Equal(1, fuelCount);
            Assert.Equal(1, perfCount);
            Assert.Equal(2, accelCount);
            Assert.Equal("Honda", manufacturer);
            Assert.Equal("Civic", modelName);
        }

        [Fact]
        public void OptionalExtras_SetOperations()
        {
            var extras = OptionalExtras.SunRoof | OptionalExtras.SportsPack;

            Assert.True((extras & OptionalExtras.SunRoof) != 0);
            Assert.True((extras & OptionalExtras.SportsPack) != 0);
            Assert.True((extras & OptionalExtras.CruiseControl) == 0);
        }

        [Fact]
        public void Model_EnumValues()
        {
            Assert.Equal((byte)'A', (byte)Model.A);
            Assert.Equal((byte)'B', (byte)Model.B);
            Assert.Equal((byte)'C', (byte)Model.C);
        }

        [Fact]
        public void BooleanType_EnumValues()
        {
            Assert.Equal((byte)0, (byte)BooleanType.F);
            Assert.Equal((byte)1, (byte)BooleanType.T);
        }
    }
}
