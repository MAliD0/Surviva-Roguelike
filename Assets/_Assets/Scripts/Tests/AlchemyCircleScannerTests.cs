using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AlchemyCircleScannerTests
{
    List<Vector2Int> positions = new List<Vector2Int>
    {
        // y = 0
        new Vector2Int(9, 0),
        new Vector2Int(10, 0),
        new Vector2Int(11, 0),
        new Vector2Int(12, 0),
        new Vector2Int(13, 0),
        new Vector2Int(14, 0),
        new Vector2Int(15, 0),

        // y = 1
        new Vector2Int(2, 1),
        new Vector2Int(3, 1),
        new Vector2Int(4, 1),
        new Vector2Int(6, 1),
        new Vector2Int(7, 1),
        new Vector2Int(9, 1),
        new Vector2Int(15, 1),

        // y = 2
        new Vector2Int(2, 2),
        new Vector2Int(4, 2),
        new Vector2Int(6, 2),
        new Vector2Int(7, 2),
        new Vector2Int(9, 2),
        new Vector2Int(11, 2),
        new Vector2Int(12, 2),
        new Vector2Int(13, 2),
        new Vector2Int(15, 2),

        // y = 3
        new Vector2Int(2, 3),
        new Vector2Int(4, 3),
        new Vector2Int(9, 3),
        new Vector2Int(11, 3),
        new Vector2Int(13, 3),
        new Vector2Int(15, 3),

        // y = 4
        new Vector2Int(2, 4),
        new Vector2Int(4, 4),
        new Vector2Int(9, 4),
        new Vector2Int(11, 4),
        new Vector2Int(13, 4),
        new Vector2Int(15, 4),

        // y = 5
        new Vector2Int(2, 5),
        new Vector2Int(3, 5),
        new Vector2Int(4, 5),
        new Vector2Int(9, 5),
        new Vector2Int(11, 5),
        new Vector2Int(13, 5),
        new Vector2Int(15, 5),

        // y = 6
        new Vector2Int(5, 6),
        new Vector2Int(6, 6),
        new Vector2Int(7, 6),
        new Vector2Int(9, 6),
        new Vector2Int(11, 6),
        new Vector2Int(12, 6),
        new Vector2Int(13, 6),
        new Vector2Int(15, 6),

        // y = 7
        new Vector2Int(5, 7),
        new Vector2Int(7, 7),
        new Vector2Int(9, 7),
        new Vector2Int(15, 7),

        // y = 8
        new Vector2Int(5, 8),
        new Vector2Int(6, 8),
        new Vector2Int(7, 8),
        new Vector2Int(9, 8),
        new Vector2Int(10, 8),
        new Vector2Int(11, 8),
        new Vector2Int(12, 8),
        new Vector2Int(13, 8),
        new Vector2Int(14, 8),
        new Vector2Int(15, 8),

        // y = 9 has no elements

        // y = 10
        new Vector2Int(1, 10),
        new Vector2Int(2, 10),
        new Vector2Int(3, 10),
        new Vector2Int(4, 10),
        new Vector2Int(5, 10),
        new Vector2Int(10, 10),
        new Vector2Int(11, 10),
        new Vector2Int(12, 10),
        new Vector2Int(13, 10),

        // y = 11
        new Vector2Int(1, 11),
        new Vector2Int(5, 11),
        new Vector2Int(10, 11),
        new Vector2Int(13, 11),

        // y = 12
        new Vector2Int(1, 12),
        new Vector2Int(3, 12),
        new Vector2Int(4, 12),
        new Vector2Int(5, 12),
        new Vector2Int(6, 12),
        new Vector2Int(7, 12),
        new Vector2Int(10, 12),
        new Vector2Int(11, 12),
        new Vector2Int(12, 12),
        new Vector2Int(13, 12),

        // y = 13
        new Vector2Int(1, 13),
        new Vector2Int(3, 13),
        new Vector2Int(5, 13),
        new Vector2Int(7, 13),
        new Vector2Int(10, 13),
        new Vector2Int(11, 13),
        new Vector2Int(12, 13),
        new Vector2Int(13, 13),

        // y = 14
        new Vector2Int(1, 14),
        new Vector2Int(2, 14),
        new Vector2Int(3, 14),
        new Vector2Int(4, 14),
        new Vector2Int(5, 14),
        new Vector2Int(6, 14),
        new Vector2Int(7, 14),

        // y = 15
        new Vector2Int(1, 15),
        new Vector2Int(2, 15),
        new Vector2Int(3, 15),
        new Vector2Int(4, 15),
        new Vector2Int(5, 15),
        new Vector2Int(6, 15),
        new Vector2Int(7, 15)
    };

    [Test]
    public void RunScanner()
    {
        AlchemyCircleScanner scanner = new();

        string result = scanner.GetLayerInfo(positions);

        Debug.Log(result);
    }
}