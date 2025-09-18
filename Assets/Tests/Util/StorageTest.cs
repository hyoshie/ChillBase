using NUnit.Framework;

public class StorageTests
{
	[Test]
	public void SaveAndLoad_Roundtrip_WorksInMemory()
	{
		using (Storage.PushInMemoryScope("TEST_"))
		{
			// Act: 保存
			Storage.SetString("my_key", "hello");

			// Act: 読み込み
			var value = Storage.GetString("my_key", "default");

			// Assert: 復元される
			Assert.AreEqual("hello", value);
		}
	}

	[Test]
	public void Delete_RemovesKey()
	{
		using (Storage.PushInMemoryScope("TEST_"))
		{
			// Arrange
			Storage.SetString("temp_key", "abc");
			Assert.IsTrue(Storage.Has("temp_key"));

			// Act
			Storage.DeleteKey("temp_key");

			// Assert
			Assert.IsFalse(Storage.Has("temp_key"));
		}
	}
}
