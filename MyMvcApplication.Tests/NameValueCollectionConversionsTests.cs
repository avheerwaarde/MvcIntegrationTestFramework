using System.Collections.Specialized;
using MvcIntegrationTestFramework;
using NUnit.Framework;

namespace MyMvcApplication.Tests
{
  [ TestFixture ]
  public class NameValueCollectionConversionsTests
  {
    [ Test ]
    public void WhenConvertingAnObjectWithOneStringPropertyToNameValueCollection( )
    {
      NameValueCollection convertedFromObjectWithString =
        NameValueCollectionConversions.ConvertFromObject( new { name = "hello" } );
      Assert.AreEqual( "hello", convertedFromObjectWithString[ "name" ] );
    }

    [ Test ]
    public void WhenConvertingAnObjectHas2PropertiesToNameValueCollection( )
    {
      NameValueCollection converted = NameValueCollectionConversions.ConvertFromObject( new { name = "hello", age = 30 } );
      Assert.AreEqual( 2, converted.Count );
      Assert.AreEqual( "hello", converted[ "name" ] );
      Assert.AreEqual( "30", converted[ "age" ] );
    }

    [ Test ]
    public void WhenConvertingAnObjectThatHasANestedAnonymousObject( )
    {
      NameValueCollection converted =
        NameValueCollectionConversions.ConvertFromObject( new { Form = new { name = "hello", age = 30 } } );

      Assert.AreEqual( 2, converted.Count );
      Assert.AreEqual( "hello", converted[ "Form.name" ] );
      Assert.AreEqual( "30", converted[ "Form.age" ] );
    }
  }
}
