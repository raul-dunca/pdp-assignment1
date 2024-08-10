using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1

{

    class Product
    {
        int id;
        int price;
        int quantity;
        Mutex mutex = new Mutex();

        public void LockMutex()
        {
            mutex.WaitOne();
        }

        public void UnlockMutex()
        {
            mutex.ReleaseMutex();
        }
        public int get_price()
        {
            return price;
        }
        public int get_id()
        {
            return id;
        }

        public int get_quantity() { return quantity; }
        public void set_quantity(int new_quantity) { quantity = new_quantity; }
        public void subtract_quantity(ref int q, ref int money, ref bool can_execute, Bill b)
        {
            mutex.WaitOne();
            can_execute = false;
            Console.WriteLine($"1) Thread: {Thread.CurrentThread.ManagedThreadId} got product {id} with PRICE:  {price}  and QUANTITY:  {quantity}");
            //Console.WriteLine($"1) Thread: {Thread.CurrentThread.ManagedThreadId} Balance: {money}");

            if (q < quantity)
            {
                quantity -= q;
                money = money + (q * price);
                can_execute = true;
                b.add_operation(new Tuple<Product, int>(this, q));
                b.add_to_total_price(q * price);
            }
            else
            {
                if (quantity > 0)
                {
                    
                    money = money + (quantity * price);
                    b.add_operation(new Tuple<Product, int>(this, quantity));
                    b.add_to_total_price(quantity * price);
                    quantity = 0;

                }
            }



            Console.WriteLine($"2) Thread: {Thread.CurrentThread.ManagedThreadId} release product {id} with PRICE:  {price}  and QUANTITY:  {quantity}");
            //Console.WriteLine($"2) Thread: {Thread.CurrentThread.ManagedThreadId} Balance: {money}");
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} with {b}");
            mutex.ReleaseMutex();
        }
        public Product(int i, int p, int q)
        {
            id = i;
            price = p;
            quantity = q;
        }

        public override string ToString()
        {
            return $"Product {id} with price: {price} and STOCK Left: {quantity}" + Environment.NewLine;
        }
    }

    class Bill
    {

        List<Tuple<Product, int>> operations= new List<Tuple<Product, int>>();
        int total_price=0;


        public override string ToString()
        {
            string output = "";
            output = "Bill with: ";
            foreach (var operation in operations)
            {
                var product = operation.Item1;
                var quantity = operation.Item2;
                output += $"(Product: {product}, Quantity: {quantity}) ";
            }
            output += $"and total price: {total_price}";
            return output + Environment.NewLine;
        }

        public List<Tuple<Product,int>> get_operations()
        {
            return operations;
        }

        public void add_operation(Tuple<Product,int> op)
        {
            operations.Add(op);
        }

        public int get_total_price()
        {
            return total_price;
        }

        public void add_to_total_price(int to_add)
        {
            total_price += to_add;
        }
    };

    class Program
    {
        static Mutex bills_mutex = new Mutex();
        static Mutex nr_threads_mutex = new Mutex();
        static int nr_of_threads = 100;
        static int max_products_on_bill = 10;
        static int max_quantity_to_take = 20;

        static void foo(ref int money, int id, List<Product> products, List<Bill> bills)
        {
            
            bool can_execute = true;

            Random random = new Random(id);
            while (can_execute == true) 
            {
                
                
                int product_cntr = random.Next() % max_products_on_bill + 1;
                Bill b = new Bill();


                int product_index = 0;
                int quantity_to_take = 0;
                Product p;
                Thread.Sleep(100);

                bills_mutex.WaitOne();
                bills.Add(b);
                bills_mutex.ReleaseMutex();
                
                while (product_cntr > 0 && can_execute == true) 
                {
                    product_index = random.Next() % products.Count;
                    quantity_to_take = random.Next() % max_quantity_to_take + 1;
                    p = products[product_index];
                    p.subtract_quantity(ref quantity_to_take, ref money, ref can_execute, b);
                    product_cntr -= 1;
                }
                

                
            }
            nr_threads_mutex.WaitOne();
            nr_of_threads -= 1;
            nr_threads_mutex.ReleaseMutex();

        }

        static void check(List<Product> products, List<Bill> bills, ref int money)
        {
            while (true)
            {
                Thread.Sleep(50);
                foreach (Product p in products)
                {
                    p.LockMutex();
                }

                bills_mutex.WaitOne();

                nr_threads_mutex.WaitOne();

                int calculated_total = 0;
                foreach (Bill b in bills)
                {
                    int total_bill_price = b.get_total_price();
                    int calculated_bill_price = 0;
                    foreach (var op in b.get_operations())
                    {

                        calculated_bill_price += op.Item1.get_price() * op.Item2;
                        
                    }

                    if (calculated_bill_price==total_bill_price)
                    {
                        Console.Write("Bill total is correct!");
                    }
                    else
                    {
                        Console.WriteLine("Bill total is WRONG!!!!!!!");
                    }
                    calculated_total += calculated_bill_price;
                }

                if (calculated_total == money)
                {
                    Console.WriteLine("Total ammount of money is correct!");
                }
                else
                {

                    Console.WriteLine("Total ammount of money is WRONG!!!!!!");

                }

                nr_threads_mutex.ReleaseMutex();

                bills_mutex.ReleaseMutex();

                foreach (var p in products)
                {
                    p.UnlockMutex();
                }

                
                if (nr_of_threads==0)
                {
                    break;
                }
            }
        }

        static void Main(string[] args)
        {
           
            int money = 0;
            List<Thread> threads = new List<Thread>();
            List<Product> products = new List<Product>();
            List<Bill> bills = new List<Bill>();
            Random rnd = new Random();
            //int nr_threads = rnd.Next();
            int nr_threads = nr_of_threads;
            int nr_products = 100;
            int price_range = 500;
            int quantity_range = 500;

            for (int i = 0; i < nr_products; i++)
            {
                Product product = new Product(i, rnd.Next() % price_range + 1, rnd.Next() % quantity_range + 1);
                products.Add(product);
            }

            for (int i = 0; i < nr_threads; i++)
            {
                int index=i;
            
                Thread thread = new Thread(() => foo(ref money, index, products, bills));
                thread.Start();
                threads.Add(thread);
            }

            Thread checkThread = new Thread(() => check(products,bills, ref money));
            checkThread.Start();

            for (int i = 0; i < nr_threads; i++)
            {
                threads[i].Join();
            }


            checkThread.Join();

            /*Console.WriteLine("ALL THE BILLS: ");
            foreach (var i in bills)
            {
                Console.WriteLine(i);
            }
            */

            Console.WriteLine($"Final money: {money}");

            Console.ReadLine();

        }
    }
}
