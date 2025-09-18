#if UNITY_INCLUDE_TESTS
public partial class RoomItemServiceSO
{
	// テスト時だけ現れる注入用フック（本番バイナリには含まれない）
	public void InjectForTests(
			RoomItemStateSO s,
			RoomStateSO rs,
			RoomDatabase db,
			RoomItemShopCatalog cat)
	{
		state = s;
		roomState = rs;
		roomDatabase = db;
		shopCatalog = cat;
	}
}
#endif
