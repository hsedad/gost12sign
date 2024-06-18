using gost12sign;
using System.Numerics;

    string message_file = "message.txt";
    string keyd_file = "keyd.dat";
    string modp_file = "modp.dat";
    string modq_file = "modq.dat";
    string rata_file = "rata.dat";
    string ratb_file = "ratb.dat";
    string coorx_file = "coorx.dat";
    string coory_file = "coory.dat";
    string signr_file = "signr.dat";
    string signs_file = "signs.dat";
    int len = 256;
    Init param;

    if (args.Length != 2) // проверяем количество аргументов консольного приложения и их корректность
    {
        Console.WriteLine("Using program: gost12sing -g key_length (256 or 512) (Initial variables)");
        Console.WriteLine("Using program: gost12sing -s key_length (256 or 512) (Signing message.txt)");
        Console.WriteLine("Using program: gost12sing -u key_length (256 or 512) (Unsigning message.txt)");
    }
    else
    {
        if (!args[0].Contains("-g") && !args[0].Contains("-s") && !args[0].Contains("-u"))
        {
            Console.WriteLine("Using program: gost12sing -g key_length (256 or 512) (Initial variables)");
            Console.WriteLine("Using program: gost12sing -s key_length (256 or 512) (Signing message.txt)");
            Console.WriteLine("Using program: gost12sing -u key_length (256 or 512) (Unsigning message.txt");
            return;
        }
        if (!args[1].Contains("256") && !args[1].Contains("512"))
        {
            Console.WriteLine("Using program: gost12sing -g key_length (256 or 512) (Initial variables)");
            Console.WriteLine("Using program: gost12sing -s key_length (256 or 512) (Signing message.txt)");
            Console.WriteLine("Using program: gost12sing -u key_length (256 or 512) (Unsigning message.txt");
            return;
        }
        if (args[0] == "-g") { len = GetLen(args[1]); InitData(len); }
        if (args[0] == "-s") { len = GetLen(args[1]); param = ReadData(); Sign(message_file, param, len); }
        if (args[0] == "-u") { len = GetLen(args[1]); param = ReadData(); Check(message_file, param, len); }
    }



    int GetLen(string s) // функция возвращает длину хэш из параметра командной строки
    {
        int res = 0;
        if (s == "256") res = 256;
        if (s == "512") res = 512;
        return res;
    }

    void InitData(int len) // метод запускает первоначальную настройку параметров
    {
        Init init = new Init(len);
        File.WriteAllBytes(modp_file, init.p.ToByteArray());
        File.WriteAllBytes(modq_file, init.q.ToByteArray());
        File.WriteAllBytes(keyd_file, init.d.ToByteArray());
        File.WriteAllBytes(rata_file, init.a.ToByteArray());
        File.WriteAllBytes(ratb_file, init.b.ToByteArray());
        File.WriteAllBytes(coorx_file, init.x.ToByteArray());
        File.WriteAllBytes(coory_file, init.y.ToByteArray());
    }

    Init ReadData() // функция считывает параметры из файлов
    {
        Init init = new Init();
        init.p = new BigInteger(File.ReadAllBytes(modp_file));
        init.q = new BigInteger(File.ReadAllBytes(modq_file));
        init.d = new BigInteger(File.ReadAllBytes(keyd_file));
        init.a = new BigInteger(File.ReadAllBytes(rata_file));
        init.b = new BigInteger(File.ReadAllBytes(ratb_file));
        init.x = new BigInteger(File.ReadAllBytes(coorx_file));
        init.y = new BigInteger(File.ReadAllBytes(coory_file));
        return init;
    }

    void Sign(string mfile, Init init, int len) // метод запускает процедуру формирования электронной подписи
{
        BigInteger k, r, s; // промежуточное число k выходные r s
        byte[] message = File.ReadAllBytes(mfile); // читаем входящее сообщение в массив байт
        GetHash h = new GetHash(message, len); // вычисляем хэш заданной длины len
        BigInteger a = new BigInteger(h.Hash); // хэш в переменную а
        Console.WriteLine("hash = " + a.ToString("X2"));
        BigInteger e = a % init.q; // начинаем формирование подписи
        if (e == 0) e = 1;
        Point P = new Point(init.x, init.y, init.a, init.b, init.q); // берем исходную точку P (в NIST это G)
        do
        {
            do
            {
                k = init.GenD(init.q, init.p); // генерируем случайное число 0 < k < q
                Point C = P.Multiply(P, k); // умножаем точку P на k
                r = C.x % init.q; // берем координату x полученной точки - старшая часть ЭЦП
            }
            while (r == 0); // проверяем на 0
            s = ((r * init.d) + (k * e)) % init.q; // формируем младшую часть ЭЦП
        }
        while (s <= 0); // проверяем на 0
        File.WriteAllBytes(signr_file, r.ToByteArray()); // записываем ЭЦП
        File.WriteAllBytes(signs_file, s.ToByteArray()); // записываем ЭЦП
        Point Q = P.Multiply(P, init.d);
        Console.WriteLine("xQ= " + Q.x.ToString("X2"));
        Console.WriteLine("yQ= " + Q.y.ToString("X2"));
        Console.WriteLine("r = " + r.ToString("X2"));
        Console.WriteLine("s = " + s.ToString("X2"));
}

    void Check(string mfile, Init init, int len) // метод запускает проверку электронной подписи
{
        BigInteger r, s; // ЭЦП к сообщению
        r = new BigInteger(File.ReadAllBytes(signr_file)); // считываем старшую часть
        s = new BigInteger(File.ReadAllBytes(signs_file)); // считываем младшую часть
        Console.WriteLine("r = " + r.ToString("X2"));
        Console.WriteLine("s = " + s.ToString("X2"));
        if ((r < 0) || (r > init.q)) { Console.WriteLine("Signature Error"); return; } // экспресс-проверка на корректность ЭЦП
        if ((s < 0) || (s > init.q)) { Console.WriteLine("Signature Error"); return; } // экспресс-проверка на корректность ЭЦП
        byte[] message = File.ReadAllBytes(mfile); // считываем сообщение в массив байт
        GetHash h = new GetHash(message, len); // вычисляем хэш сообщения
        BigInteger a = new BigInteger(h.Hash); // хэш в переменную а
        Console.WriteLine("hash = " + a.ToString("X2"));
        BigInteger e = a % init.q; // начинаем проверку подписи
        if (e == 0) e = 1;
        BigInteger v = init.GetReverse(e,init.q); // v обратный элемент e
        BigInteger z1 = (s * v) % init.q; // множитель z1
        BigInteger z2 = init.q + ((-(r * v)) % init.q); // множитель z2
        Point P = new Point(init.x, init.y, init.a, init.b, init.q); // исходная точка P
        Point Q = P.Multiply(P, init.d); // точка созданная при формировании ЭЦП
        Console.WriteLine("xQ= " + Q.x.ToString("X2"));
        Console.WriteLine("yQ= " + Q.y.ToString("X2"));
        Point Z1 = P.Multiply(P, z1); // точка Z1
        Point Z2 = Q.Multiply(Q, z2); // точка Z2
        Point С = Z1 + Z2; // cумма точек
        Console.WriteLine("xС= " + С.x.ToString("X2"));
        Console.WriteLine("yС= " + С.y.ToString("X2"));
        BigInteger R = С.x % init.q; // получаем значение
        Console.WriteLine("R = " + R.ToString("X2"));
        if ( R == r ) Console.WriteLine("Signature is correct"); // если получили значение, то подпись и сообщение истинны
        else Console.WriteLine("Signature is incorrect"); // иначе подпись или сообщение искажены
    }

    //InitData(256); // инициализируем данные и записываем их в файлы
    param = ReadData(); // читаем их из файлов
    Console.WriteLine("Исходные данные");
    Console.WriteLine("p = " + param.p.ToString("X2"));
    Console.WriteLine("q = " + param.q.ToString("X2"));
    Console.WriteLine("a = " + param.a.ToString("X2"));
    Console.WriteLine("b = " + param.b.ToString("X2"));
    Console.WriteLine("d = " + param.d.ToString("X2"));
    Console.WriteLine("xP= " + param.x.ToString("X2"));
    Console.WriteLine("yP= " + param.y.ToString("X2"));
    Sign(message_file, param, len); // формируем ЭЦП для сообщения
    Console.WriteLine("Результаты проверки");
    Check(message_file, param, len); // проверяем ЭЦП под сообщением



