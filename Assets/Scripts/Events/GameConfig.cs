// Файл: Assets/Scripts/Core/GameConfig.cs
using UnityEngine;

public static class GameConfig
{
    // Колода
    public const int MIN_DECK_SIZE = 2;
    public const int MAX_DECK_SIZE = 30;

    // Бой
    public const int TOTAL_ROUNDS = 5;
    public const int WINS_NEEDED = 3;
    public const int STARTING_ENERGY = 3;

    // Рука
    public const int HAND_SIZE = 4;
    public const float CARD_WIDTH = 150f;
    public const float CARD_HEIGHT = 200f;

    // Карта мира
    public const int MAP_LEVELS = 5;
    public const int MAX_BRANCHES = 3;
}