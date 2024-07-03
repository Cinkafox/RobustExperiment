namespace Content.Shared.GameTicking;

public sealed class MazeGenerator
{
    private int width, height;
    private int[,] maze;
    private Random rand = new Random();

    public MazeGenerator(int width, int height)
    {
        this.width = width;
        this.height = height;
        maze = new int[height, width];
    }

    public int[,] Generate()
    {
        // Заполнить лабиринт стенами
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                maze[y, x] = 1;
            }
        }

        // Начальная точка
        int startX = rand.Next(width / 2) * 2;
        int startY = rand.Next(height / 2) * 2;
        maze[startY, startX] = 0;

        GeneratePath(startX, startY);

        return maze;
    }

    private void GeneratePath(int x, int y)
    {
        // Случайный порядок направления движения
        int[] directions = new int[] { 0, 1, 2, 3 };
        Shuffle(directions);

        foreach (var direction in directions)
        {
            int newX = x, newY = y;

            switch (direction)
            {
                case 0: newY -= 2; break; // Вверх
                case 1: newY += 2; break; // Вниз
                case 2: newX -= 2; break; // Влево
                case 3: newX += 2; break; // Вправо
            }

            if (IsInMaze(newX, newY) && maze[newY, newX] == 1)
            {
                maze[newY, newX] = 0;
                maze[(newY + y) / 2, (newX + x) / 2] = 0;
                GeneratePath(newX, newY);
            }
        }
    }

    private bool IsInMaze(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private void Shuffle(IList<int> array)
    {
        for (var i = array.Count - 1; i > 0; i--)
        {
            var j = rand.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}