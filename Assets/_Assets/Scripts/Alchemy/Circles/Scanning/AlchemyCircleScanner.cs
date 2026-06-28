using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using UnityEngine.SocialPlatforms;

public class AlchemyCircleScanner
{
    public string GetLayerInfo(List<Vector2Int> positions)
    {
        int[,] grid = new int[16,16];

        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            grid[pos.x, pos.y] = 1;
        }
        
        int groupcount = 0;
        List<Group> groups = new List<Group>();

        while(positions.Count != 0)
        {
            Vector2Int firstElement = positions[0];

            Group newGroup = new Group();

            newGroup.localIndex = groupcount;
            newGroup.TryAddUnique(firstElement);

            WaveFunction(firstElement, grid, ref newGroup);

            foreach (var coordinate in newGroup.coordinates)
            {
                positions.Remove(coordinate);
            }

            groups.Add(newGroup);
            
            groupcount ++;
        }


        string result = "";
        
        foreach (var group in groups)
        {
            result += $"index: {group.localIndex} size: {group.coordinates.Count.ToString()}";
            result += "\n";
        }

        Debug.Log("\n" + result);

        return result;
    }

    private void WaveFunction(
        Vector2Int coordinate,
        int[,] grid,
        ref Group newGroup)
    {
        List<Vector2Int> neighbours = getNeighbourTiles(coordinate, grid).ToList();

        foreach (Vector2Int neighbour in neighbours)
        {
            if (!newGroup.TryAddUnique(neighbour))
            {
                continue;
            }

            WaveFunction(neighbour, grid, ref newGroup);
        }
    }

    struct Group
    {
        public List<Vector2Int> coordinates;
        public int localIndex;

        public bool TryAddUnique(Vector2Int vector2Int)
        {
            if(coordinates == null) 
                coordinates = new List<Vector2Int>();

            if (!coordinates.Contains(vector2Int))
            {
                coordinates.Add(vector2Int);
                return true;
            }

            return false;
        }
    }

    public Vector2Int[] getNeighbourTiles(Vector2Int coordinate, int[,] positions)
    {
        Vector2Int[] directions = {Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right};
        
        int maxX = positions.GetLength(0);
        int maxY = positions.GetLength(1);

        List<Vector2Int> occupiedPositions = new List<Vector2Int>();

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int dir = directions[i];

            Vector2Int newDir = coordinate + dir;

            if(newDir.x >= maxX ||  newDir.x < 0 || newDir.y >= maxY || newDir.y < 0)
                continue;

            if(positions[newDir.x, newDir.y] != 0)
            {
                occupiedPositions.Add(newDir);
            }
        }

        return occupiedPositions.ToArray();
    }


    private string GridToString(int[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        StringBuilder builder = new StringBuilder();

        // Highest Y first, so the grid is not vertically inverted.
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                builder.Append(grid[x, y] == 0 ? '·' : '█');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
