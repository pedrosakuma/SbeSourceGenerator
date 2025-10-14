using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using System.Linq;
using System.Xml;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class ValidationGeneratorTests
    {
        [Fact]
        public void Generate_WithMinMaxConstraints_ProducesValidationCode()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <types>
        <type name='Price' primitiveType='int64' minValue='0' maxValue='999999999'/>
    </types>
    <sbe:message name='Order' id='1'>
        <field name='price' id='1' type='int64' minValue='0' maxValue='999999999'/>
    </sbe:message>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.NotEmpty(results);
            
            // Check message validation was generated
            var orderValidation = results.FirstOrDefault(r => r.name.Contains("OrderValidation"));
            Assert.True(orderValidation != default);
            Assert.Contains("OrderValidation", orderValidation.content);
            Assert.Contains("Validate", orderValidation.content);
            Assert.Contains("ArgumentOutOfRangeException", orderValidation.content);
            Assert.Contains("0", orderValidation.content);
            Assert.Contains("999999999", orderValidation.content);
            
            // Check type validation was generated
            var priceValidation = results.FirstOrDefault(r => r.name.Contains("PriceValidation"));
            Assert.True(priceValidation != default);
            Assert.Contains("PriceValidation", priceValidation.content);
            Assert.Contains("Validate", priceValidation.content);
        }

        [Fact]
        public void Generate_WithoutConstraints_DoesNotProduceValidationCode()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <types>
        <type name='SimpleType' primitiveType='int64'/>
    </types>
    <sbe:message name='SimpleMessage' id='1'>
        <field name='simpleField' id='1' type='int64'/>
    </sbe:message>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void Generate_WithMinValueOnly_ProducesMinValidationCode()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <types>
        <type name='Quantity' primitiveType='int64' minValue='1'/>
    </types>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.Single(results);
            var quantityValidation = results.First();
            Assert.Contains("QuantityValidation", quantityValidation.content);
            Assert.Contains("must be greater than or equal to 1", quantityValidation.content);
        }

        [Fact]
        public void Generate_WithMaxValueOnly_ProducesMaxValidationCode()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <types>
        <type name='LimitedValue' primitiveType='int32' maxValue='100'/>
    </types>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.Single(results);
            var limitedValidation = results.First();
            Assert.Contains("LimitedValueValidation", limitedValidation.content);
            Assert.Contains("must be less than or equal to 100", limitedValidation.content);
        }

        [Fact]
        public void Generate_MessageWithMultipleConstraints_ProducesValidationForAll()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <sbe:message name='ComplexOrder' id='1'>
        <field name='price' id='1' type='int64' minValue='0' maxValue='999999999'/>
        <field name='quantity' id='2' type='int64' minValue='1'/>
        <field name='discount' id='3' type='int32' maxValue='100'/>
    </sbe:message>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.Single(results);
            var validation = results.First();
            Assert.Contains("ComplexOrderValidation", validation.content);
            Assert.Contains("Price", validation.content);
            Assert.Contains("Quantity", validation.content);
            Assert.Contains("Discount", validation.content);
        }

        [Fact]
        public void Generate_WithConstraints_ProducesTryValidateMethod()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <types>
        <type name='Price' primitiveType='int64' minValue='0' maxValue='999999999'/>
    </types>
    <sbe:message name='Order' id='1'>
        <field name='price' id='1' type='int64' minValue='0' maxValue='999999999'/>
    </sbe:message>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.NotEmpty(results);
            
            // Check message TryValidate was generated
            var orderValidation = results.FirstOrDefault(r => r.name.Contains("OrderValidation"));
            Assert.True(orderValidation != default);
            Assert.Contains("TryValidate", orderValidation.content);
            Assert.Contains("out string? errorMessage", orderValidation.content);
            Assert.Contains("return true", orderValidation.content);
            Assert.Contains("return false", orderValidation.content);
            
            // Check type TryValidate was generated
            var priceValidation = results.FirstOrDefault(r => r.name.Contains("PriceValidation"));
            Assert.True(priceValidation != default);
            Assert.Contains("TryValidate", priceValidation.content);
            Assert.Contains("out string? errorMessage", priceValidation.content);
        }

        [Fact]
        public void Generate_WithConstraints_ProducesCreateValidatedMethod()
        {
            // Arrange
            var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='test'
                   id='1'
                   version='0'>
    <types>
        <type name='Price' primitiveType='int64' minValue='0' maxValue='999999999'/>
    </types>
    <sbe:message name='Order' id='1'>
        <field name='price' id='1' type='int64' minValue='0' maxValue='999999999'/>
    </sbe:message>
</sbe:messageSchema>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationGenerator();

            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default).ToList();

            // Assert
            Assert.NotEmpty(results);
            
            // Check message CreateValidated was generated
            var orderValidation = results.FirstOrDefault(r => r.name.Contains("OrderValidation"));
            Assert.True(orderValidation != default);
            Assert.Contains("CreateValidated", orderValidation.content);
            Assert.Contains("message.Validate()", orderValidation.content);
            Assert.Contains("return message", orderValidation.content);
            
            // Check type CreateValidated was generated
            var priceValidation = results.FirstOrDefault(r => r.name.Contains("PriceValidation"));
            Assert.True(priceValidation != default);
            Assert.Contains("CreateValidated", priceValidation.content);
            Assert.Contains("value.Validate()", priceValidation.content);
            Assert.Contains("return value", priceValidation.content);
        }
    }
}
