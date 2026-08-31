public interface IAttackSpeedBoostable
{
    // 添加攻击速度加成
    // 攻撃速度ボーナスを追加する
    void AddAttackSpeedBoost(float multiplier);

    // 移除攻击速度加成
    // 攻撃速度ボーナスを解除する
    void RemoveAttackSpeedBoost(float multiplier);
}
