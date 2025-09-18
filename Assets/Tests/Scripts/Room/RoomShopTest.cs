using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript
{
    RoomShopServiceSO _svc;
    RoomStateSO _state;

    [SetUp]
    public void SetUp()
    {
        // 永続のクリーンアップ
        PlayerPrefs.DeleteKey(RoomConstants.KeyOwnedRooms);
        PlayerPrefs.DeleteKey(RoomConstants.KeyCurrentRoom);
        PlayerPrefs.Save();

        // コインも既知値に（念のため）
        CurrencyManager.SetCoins(0);

        // テスト用 SO を生成 & 注入
        _svc = ScriptableObject.CreateInstance<RoomShopServiceSO>();
        _state = ScriptableObject.CreateInstance<RoomStateSO>();
        _svc.InjectState(_state);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_svc);
        Object.DestroyImmediate(_state);
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [Test]
    public void EnsureDefault_SetsRepository_And_State()
    {
        string targetRoomId = RoomConstants.DefaultRoomId;
        _svc.EnsureDefault(targetRoomId);

        // Repository 側
        Assert.AreEqual(targetRoomId, RoomRepository.LoadCurrent());
        // StateSO 側
        Assert.AreEqual(targetRoomId, _state.CurrentId);
    }

    [Test]
    public void Use_Updates_State_And_Persists_Current()
    {
        _svc.EnsureDefault(RoomConstants.DefaultRoomId);
        string targetRoomId = RoomConstants.RoomFr2020;
        _svc.Use(RoomConstants.RoomFr2020);

        Assert.AreEqual(targetRoomId, _state.CurrentId);
        Assert.AreEqual(targetRoomId, RoomRepository.LoadCurrent());
    }

    [Test]
    public void TryBuy_AddsOwnership_WhenEnoughCoins()
    {
        CurrencyManager.SetCoins(100);

        string targetRoomId = RoomConstants.RoomFr2020;
        var ok = _svc.TryBuy(targetRoomId, price: 100);

        Assert.IsTrue(ok);

        var owned = RoomRepository.LoadOwned();
        Assert.IsTrue(owned.Contains(targetRoomId));
    }

    [Test]
    public void TryBuy_Fails_WhenNotEnoughCoins()
    {
        CurrencyManager.SetCoins(99);

        string targetRoomId = RoomConstants.RoomFr2020;
        var ok = _svc.TryBuy(targetRoomId, price: 100);

        Assert.IsFalse(ok);

        var owned = RoomRepository.LoadOwned();
        Assert.IsFalse(owned.Contains(targetRoomId));
    }

    //（必要なら残すサンプル）
    [UnityTest]
    public IEnumerator SceneShopTestWithEnumeratorPasses()
    {
        yield return null;
    }
}
// // A Test behaves as an ordinary method
// [Test]
// public void NewTestScriptSimplePasses()
// {
//     // Use the Assert class to test conditions
// }

// // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
// // `yield return null;` to skip a frame.
// [UnityTest]
// public IEnumerator NewTestScriptWithEnumeratorPasses()
// {
//     // Use the Assert class to test conditions.
//     // Use yield to skip a frame.
//     yield return null;
// }
