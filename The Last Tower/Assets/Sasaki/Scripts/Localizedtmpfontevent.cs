using UnityEngine;
using UnityEngine.Localization;
using TMPro;

/// <summary>
/// 言語ごとにTMP_FontAssetを切り替えるためのコンポーネント
/// （Localize TMP Font Asset Event が存在しない場合の自作版）
///
/// 【使い方】
/// 1. Localization Tablesで「Asset Table Collection」を作成（例：Fonts）
/// 2. 各言語ごとにTMP_FontAssetを登録
/// 3. フォントを切り替えたいTextMeshProオブジェクトにこのコンポーネントを追加
/// 4. Inspectorの Asset Reference に、Asset Tableのエントリを設定
///    （Table = 作成したAsset Table Collection名、Entry = 登録したキー）
/// 5. Target Text は未アサインなら自身のTMP_Textを自動取得する
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedTMPFontEvent : MonoBehaviour
{
    [Header("── ローカライズ参照 ────────────")]
    [Tooltip("Asset Table Collectionの中の、フォントを登録したエントリを指定する")]
    public LocalizedAsset<TMP_FontAsset> assetReference = new LocalizedAsset<TMP_FontAsset>();

    [Header("── 対象テキスト ────────────────")]
    [Tooltip("未アサインの場合、自身にアタッチされたTMP_Textを自動取得")]
    public TMP_Text targetText;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        assetReference.AssetChanged += OnAssetChanged;
        // 現在選択中の言語ですぐに反映させる
        assetReference.LoadAssetAsync();
    }

    void OnDisable()
    {
        assetReference.AssetChanged -= OnAssetChanged;
    }

    void OnAssetChanged(TMP_FontAsset font)
    {
        if (font == null || targetText == null) return;
        targetText.font = font;
    }
}