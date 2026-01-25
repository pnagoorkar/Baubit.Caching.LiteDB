using LiteDB;

namespace Baubit.Caching.LiteDB.Test.Misc
{
    public class NullableGuidTest
    {
        [Fact]
        public void LiteDB_CanSerializeNullableGuid()
        {
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nullable_guid_test_{Guid.NewGuid()}.db");
            
            try
            {
                var testGuid = Guid.NewGuid();
                
                // Write
                using (var db = new LiteDatabase(dbPath))
                {
                    var col = db.GetCollection<EnumeratorPosition<Guid>>("test");
                    col.Insert(new EnumeratorPosition<Guid>("session-1", testGuid));
                    db.Commit();
                }
                
                // Read
                using (var db = new LiteDatabase(dbPath))
                {
                    var col = db.GetCollection<EnumeratorPosition<Guid>>("test");
                    var retrieved = col.FindById("session-1");
                    
                    Assert.NotNull(retrieved);
                    Assert.Equal(testGuid, retrieved.CurrentId);
                }
            }
            finally
            {
                if (System.IO.File.Exists(dbPath))
                    System.IO.File.Delete(dbPath);
            }
        }
    }
}
