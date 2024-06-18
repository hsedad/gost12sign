namespace gost12sign
{
    internal class GetHash
    {
        public byte[] Hash; // поле для размещения хэш значения
        PreSet Pre = new PreSet(); // класс содержит массивы значений Pi, Tau, A, C, заданных ГОСТ 34.11-2012
        
        public GetHash() { Hash = new byte[512]; } // конструктор без параметров, для отладки методов класса

        public GetHash(byte[] Message, int hash_size) // конструктор класса, вычисляет хэш сообщения Message размером hash_size
        {
            Hash = new byte[hash_size]; // инициализируем массив для размещения хеш значения
            byte[] hash = FillBytes(64, 0); // массив для размещения промежуточных значений хэш
            byte[] v_0 = FillBytes(64, 0); // инициализационный вектор v_0
            byte[] v_512 = FillBytes(64, 0); // инициализационный вектор v_512
            byte[] N = FillBytes(64, 0); // промежуточный вектор
            byte[] message = FillBytes(64, 0); // блок сообщения 512 бит
            byte[] Sigma = FillBytes(64, 0); // контрольная сумма сообщения
            byte[] temp = FillBytes(64, 0); // промежуточный вектор
    
            if (hash_size == 256) hash = FillBytes(64, 1); // если 256-бит хэш, задаем значения исходного вектора в 1
            if (hash_size == 512) v_512[1] = 0x2; // если 512-бит хэш, инициализируем вектор v_512 значением 512

            int len = Message.Length; // длина входящего сообщения в итератор len
            int i = 0; // итератор цикла
            while (len > 64) // работаем с 512-бит блоками, если их больше одного
            {
                Array.Copy(Message, i * 64, message, 0, 64); // выделяем очередные 512 бит сообщения
                temp = TurnG(hash, N, message); // используем функцию сжатия G
                N = Add_512(N, v_512); // перерасчет значения промежуточного вектора N
                Sigma = Add_512(Sigma, temp); // перерасчет значения контрольной суммы Sigma
                len -= 64; i++; // итераторы для следующего цикла
            }

            if (len <= 64) // работаем с последним (или единственным) блоком сообщения
            {
                message = FillTail(Message); // получаем нормализованный "хвост" сообщения 
                temp = TurnG(hash, N, message); // используем функцию сжатия G
                N = Add_512(N, v_512); // перерасчет значения промежуточного вектора N
                Sigma = Add_512(Sigma, temp); // перерасчет значения контрольной суммы Sigma
                hash = TurnG(hash, v_0, N); // перерасчет промежуточного значения hash (сжатие G)
                hash = TurnG(hash, v_0, Sigma); // перерасчет промежуточного значения hash (сжатие G)
            }

            if (hash_size == 512) Hash = hash; // если размер хэш 512 бит, то возвращаем значение
            if (hash_size == 256) // если размер хэш 256 бит, то возвращаем младшие 256 бит значения
            {
                byte[] hash256 = new byte[32]; // вектор 256 бит хэш
                Array.Copy(hash,0, hash256, 0, 32); // выделяем младшие 32 байта
                Hash = hash256; // возвращаем значение
            } 
        }

        public byte[] Add_2(byte[] a, byte[] b) // функция сложения двух 512 битных блока a и b по модулю 2
        {
            byte[] result = new byte[64];
            for (int i = 0; i < 64; i++) { result[i] = (byte) (a[i] ^ b[i]); }
            return result;
        }

        public byte[] Add_512(byte[] a, byte[] b) // функция возвращает побитовое исключающее или двух 512 битных блоков a и b
        {                                         // (сложение в кольце вычетов по модулю 2 в степени n)
            byte[] result = new byte[64];
            int temp = 0; // промежуточное значение
            for (int i = 0;i < 64;i++) // берем байты попарно
            {
                temp = a[i] + b[i] + (temp >> 8); // получаем значение байта (таким он должен быть и при сложении больших чисел)
                result[i] = (byte)(temp & 0xff); // отбрасываем переполнение
            }
            return result; // возращаем значение
        }

        public byte[] TurnS(byte[] a) // функция осуществляет нелинейное биективное преобразование S 512 битного блока a
        {
            byte[] result = new byte[64];
            for (int i = 63; i >= 0; i--) { result[i] = Pre.Pi[a[i]]; } // осуществляем преобразование используя ГОСТ вектор Pi
            return result; // возвращаем результат
        }

        public byte[] TurnP(byte[] a) // функция осуществляет перестановку байт 512 битного блока a (преобразование P)
        {
            byte[] result = new byte[64];
            for (int i=0; i < 64; i++) { result[i] = a[Pre.Tau[i]]; } // осуществляем перестановку используя ГОСТ вектор Tau
            return result; // возвращаем результат
        }

        public byte[] TurnL(byte[] a) // функция осуществляет линейное преобразование L 512-бит блока a
        {
            ulong[] temp_in = new ulong[8]; // входной массив из 64-бит значений
            ulong[] temp_ou = new ulong[8]; // выходной массив из 64-бит значений
            for (int i = 0; i < 8; i++) temp_in[i] = UlongFromBytes(a, i*8); // заполняем входной массив 
            for (int i = 0; i < 8; i++) temp_ou[i] = 0; // инициализируем выходной массив
            for (int i = 0; i < 8; i++) // берем каждый 64-бит блок
            {
                for (int j = 0; j < 64; j++) // проходим по каждому биту 64-бит блока
                {
                    if (((temp_in[i] >> j) & 1) == 1) // если очередной бит равен 1
                    {
                        temp_ou[i] ^= Pre.A[j]; // то значение результирующего вектора ксорится со значением вектора A
                    }
                }
            }
            byte[] result = new byte[64]; // выходной массив 64 байт
            for (int i = 0; i < 8; i++) 
                for (int j = 0; j < 8; j++) result[i*8+j] = BytesFromUlong(temp_ou[i])[j]; // извлекаем байты из векторов 64-бит значений
            return result; //возвращаем результат
        }

        public byte[] GetK(byte[] a, int i) // функция возвращает 512 битный раундовый ключ k для преобразования E
        {
            byte[] result = new byte[64]; // выходной массив 64 байт
            result = Add_2(a, Pre.C[i]); // складываем 512-бит вектор a с 512-бит i вектором матрицы C
            result = TurnS(result); // осуществляем биективное преобразование результата
            result = TurnP(result); // осуществляем перестановку байт результата
            result = TurnL(result); // осуществляем линейное преобразование результата
            return result;   // возвращаем результат
        }

        public byte[] TurnE(byte[] k, byte[] m) // функция осуществляет преобразование E используя сообщение m и сессионный ключ k
        {
            byte[] K = k; // промежуточный вектор для сессионых ключей
            byte[] result = new byte[64]; // выходной массив 64 байт
            result = Add_2(m, K); // складываем сообщение m и сессионый ключ K
            for (int i = 0; i <12 ; i++) // перебираем матрицу итерационных констант C
            {
               result = TurnS(result); // осуществляем биективное преобразование результата
               result = TurnP(result); // осуществляем перестановку байт результата
               result = TurnL(result); // осуществляем линейное преобразование результата
               K = GetK(K, i); // вычисляем очередной сессионый ключ
               result = Add_2(result, K); // прибавляем сессионный ключ к результату             
            }
            return result; // возращаем результат
        }

        public byte[] TurnG(byte[] h, byte[] N, byte[] m) // функция сжатия G возвращает значение hash
        {
            byte[] K = Add_2(N, h); // инициализируем значение сессионого ключа
            byte[] result = new byte[64]; // выходной массив 64 байт
            K = TurnS(K); // осуществляем биективное преобразование ключа
            K = TurnP(K); // осуществляем перестановку байт ключа
            K = TurnL(K); // осуществляем линейное преобразование ключа
            result = TurnE(K, m); // осуществляем преобразование E для блока сообщения и ключа
            result = Add_2(result, h); // сжимаем значение хэш
            result = Add_2(result, m); // сжимаем значение сообщения
            return result;
        }

        public byte[] FillTail(byte[] m) // функция возвращает "хвост" сообщения, приведенного до длины в 512 бит
        {
            int tail = 0;  // длина "хвоста" исходного сообщения
            int len = m.Length; // общая длина сообщения
            int lot = (len- 1) / 64; // количество блоков в сообщении минус 1
            if (len > 64) { tail = len % 64; } // если сообщение длинее 64 байт, то вычисляем длину последнего блока
            else { tail = len; } // иначе длина последнего блока равна длине сообщения
            byte[] result = new byte[64]; // выходной массив 64 байт
            for (int i = 0; i < tail; i++) { result[i] = m[(64*lot) + i];} // заполняем значениями из исходного сообщения
            for (int i = tail; i < 64; i++) { result[i] = 0; } // добавляем нулевые значения
            if (tail != 64) { result[63] = 1; } // в последний байт записываем 1
            return result;
        }

        public byte[] FillBytes(int len, byte val) // функция возвращает массив байт длиной len заполненый значениями val
        {
            byte[] bytes = new byte[len];
            for (int i = 0; i < len; i++) bytes[i] = val;
            return bytes;
        }

        public ulong UlongFromBytes(byte[] input, int index) // метод собирает 64 битовое число из массива байт[8]
        {
            ulong result = 0;
            result |= input[index];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 1];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 2];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 3];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 4];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 5];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 6];
            result <<= 8; result &= 0xffffffffffffff00;
            result |= input[index + 7];
            return result;
        }

        public byte[] BytesFromUlong(ulong input)  // метод раскладывает 64 битное число в массив байт[8]
        {
            byte[] result = new byte[8];
            result[7] = (byte)input;
            result[6] = (byte)(input >> 8);
            result[5] = (byte)(input >> 16);
            result[4] = (byte)(input >> 24);
            result[3] = (byte)(input >> 32);
            result[2] = (byte)(input >> 40);
            result[1] = (byte)(input >> 48);
            result[0] = (byte)(input >> 56);
            return result;
        }
    }
}
