using System.Numerics;

namespace gost12sign
{
    public class Point
    {
        internal BigInteger a; // коэфициент a уравнения эллиптической кривой
        internal BigInteger b; // коэфициент b уравнения эллиптической кривой
        internal BigInteger x; // координата точки ось x
        internal BigInteger y; // координата точки ось y
        internal BigInteger mod;  // модуль эллиптической кривой

        public Point() { } // пустой конструктор, для доступа к методам

        public Point(BigInteger x, BigInteger y, BigInteger a, BigInteger b, BigInteger mod) // создаем точку
        {
            this.x = x;
            this.y = y;
            this.a = a;
            this.b = b;
            this.mod = mod;
        }

        public static Point operator+(Point p1, Point p2) // оператор сложения двух разных точек p1 и p2
        {
            BigInteger mod = p1.mod; // модуль 
            Init init = new Init(); // класс, содержащий методы работы с элементами кривой
            Point res = new Point(); // новая точка (результат сложения)
            res.a = p1.a; // переносим параметры эллиптической кривой
            res.b = p1.b; // переносим параметры эллиптической кривой
            res.mod = mod; // переносим параметры эллиптической кривой

            BigInteger dx = p2.x - p1.x; // знаменатель коэффициента лямбда
            BigInteger dy = p2.y - p1.y; // числитель коэффициента лямбда
            if (dx < 0) dx += mod; // нужны положительные значения
            if (dy < 0) dy += mod; // нужны положительные значения

            BigInteger t = (dy * init.GetReverse(dx, mod)) % mod; // t это коэффициент лямбда в алгоритме сложения точек
            if (t < 0) t += mod; // нужны положительные значения

            res.x = (t * t - p1.x - p2.x) % mod; // вычисляем x
            res.y = (t * (p1.x - res.x) - p1.y) % mod; // вычисляем y
            if (res.x < 0) res.x += mod; // нужны положительные значения
            if (res.y < 0) res.y += mod; // нужны положительные значения
            return res; // возвращаем точку
        }

        public Point Double(Point p) // функция удвоения точки
        {
            BigInteger mod = p.mod; // модуль
            Init init = new Init(); // класс, содержащий методы работы с элементами кривой
            Point res = new Point(); // новая точка (результат удвоения)
            res.a = p.a; // переносим параметры эллиптической кривой
            res.b = p.b; // переносим параметры эллиптической кривой
            res.mod = mod; // переносим параметры эллиптической кривой

            BigInteger dx = 2 * p.y; // знаменатель коэффициента лямбда
            BigInteger dy = 3 * p.x * p.x + p.a; // числитель коэффициента лямбда
            if (dx < 0) dx += mod; // нужны положительные значения
            if (dy < 0) dy += mod; // нужны положительные значения

            BigInteger t = (dy * (init.GetReverse(dx,mod)) % mod); // t это коэффициент лямбда в алгоритме сложения точек
            res.x = (t * t - p.x - p.x) % mod; // вычисляем x 
            res.y = (t * (p.x - res.x) - p.y) % mod; // вычисляем y
            if (res.x < 0) res.x += mod; // нужны положительные значения
            if (res.y < 0) res.y += mod; // нужны положительные значения
            return res; // возвращаем точку
        }

        public Point Multiply(Point p, BigInteger n) // функция умножения точки на число
        {
            Point res, tmp; // промежуточные точки
            res = p; tmp = p; // берем исходную точку
            BigInteger i = n - 1; // берем итератор
            while (i > 0) // запускаем цикл
            {
                if ((i % 2) != 0) // если итератор нечетный (т.е. нужно добавить одну точку)
                {
                    if ((res.x == tmp.x) || (res.y == tmp.y)) res = Double(res); // если точки для сложения равны, удваиваем результирующую
                    else res = res + tmp; // иначе складываем две разных точки
                    i--; // уменьшаем итератор
                }
                i /= 2; // уменьшаем итератор в 2 раза
                tmp = Double(tmp); // и формируем удвоенную точку для последующих итераций
            }
            return res; // возвращаем результат
        }
    }
}
