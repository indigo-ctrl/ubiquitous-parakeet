using System;
using System.Collections.Generic;
using System.Linq;

namespace BankSimulator
{
    // Класс для хранения информации о клиенте
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public List<BankAccount> Accounts { get; set; }
        public List<Loan> Loans { get; set; }
        
        public Client(int id, string name)
        {
            Id = id;
            Name = name;
            Balance = 0;
            Accounts = new List<BankAccount>();
            Loans = new List<Loan>();
        }
    }

    // Класс для банковского счета
    public class BankAccount
    {
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; } // годовая процентная ставка
        public string AccountType { get; set; } // "Депозит", "Расчетный", "Накопительный"
        public DateTime OpenDate { get; set; }
        
        public BankAccount(string accountNumber, decimal initialBalance, decimal interestRate, string accountType)
        {
            AccountNumber = accountNumber;
            Balance = initialBalance;
            InterestRate = interestRate;
            AccountType = accountType;
            OpenDate = DateTime.Now;
        }
        
        public void Deposit(decimal amount)
        {
            Balance += amount;
        }
        
        public bool Withdraw(decimal amount)
        {
            if (Balance >= amount)
            {
                Balance -= amount;
                return true;
            }
            return false;
        }
        
        public void ApplyInterest()
        {
            Balance += Balance * (InterestRate / 100 / 12); // ежемесячные проценты
        }
    }

    // Класс для кредита
    public class Loan
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
        public decimal MonthlyPayment { get; set; }
        public int MonthsPaid { get; set; }
        public DateTime StartDate { get; set; }
        
        public Loan(int id, decimal amount, decimal interestRate, int termMonths)
        {
            Id = id;
            Amount = amount;
            InterestRate = interestRate;
            TermMonths = termMonths;
            MonthsPaid = 0;
            StartDate = DateTime.Now;
            
            // Расчет аннуитетного платежа
            decimal monthlyRate = interestRate / 100 / 12;
            MonthlyPayment = amount * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), termMonths)) 
                            / (decimal)(Math.Pow((double)(1 + monthlyRate), termMonths) - 1);
        }
        
        public bool MakePayment()
        {
            if (MonthsPaid < TermMonths)
            {
                MonthsPaid++;
                return true;
            }
            return false;
        }
        
        public decimal GetRemainingBalance()
        {
            if (MonthsPaid >= TermMonths) return 0;
            
            decimal monthlyRate = InterestRate / 100 / 12;
            int remainingMonths = TermMonths - MonthsPaid;
            
            return MonthlyPayment * ((decimal)Math.Pow((double)(1 + monthlyRate), remainingMonths) - 1) 
                   / (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), remainingMonths));
        }
    }

    // Основной класс банка
    public class Bank
    {
        public string Name { get; set; }
        public decimal Capital { get; set; }
        public decimal ReserveFund { get; set; }
        public List<Client> Clients { get; set; }
        private int nextClientId = 1;
        private int nextLoanId = 1;
        private int nextAccountNumber = 1000001;
        
        public Bank(string name, decimal initialCapital)
        {
            Name = name;
            Capital = initialCapital;
            ReserveFund = initialCapital * 0.1m; // 10% от капитала в резервный фонд
            Clients = new List<Client>();
        }
        
        public Client AddClient(string name)
        {
            var client = new Client(nextClientId++, name);
            Clients.Add(client);
            return client;
        }
        
        public BankAccount OpenAccount(Client client, decimal initialDeposit, string accountType)
        {
            decimal interestRate = accountType switch
            {
                "Депозит" => 5.0m,
                "Накопительный" => 3.5m,
                _ => 0.5m // Расчетный счет
            };
            
            var account = new BankAccount($"ACC{nextAccountNumber++}", initialDeposit, interestRate, accountType);
            client.Accounts.Add(account);
            client.Balance += initialDeposit;
            Capital += initialDeposit;
            
            return account;
        }
        
        public Loan GrantLoan(Client client, decimal amount, decimal interestRate, int termMonths)
        {
            if (amount > Capital * 0.1m) // Не более 10% от капитала банка на одного клиента
            {
                Console.WriteLine("Заявка на кредит отклонена: сумма превышает лимит");
                return null;
            }
            
            if (client.Balance < amount * 0.2m) // Требуется 20% собственных средств
            {
                Console.WriteLine("Заявка на кредит отклонена: недостаточно собственных средств");
                return null;
            }
            
            var loan = new Loan(nextLoanId++, amount, interestRate, termMonths);
            client.Loans.Add(loan);
            client.Balance += amount;
            Capital -= amount;
            
            return loan;
        }
        
        public void ProcessMonth()
        {
            // Начисление процентов по счетам
            foreach (var client in Clients)
            {
                foreach (var account in client.Accounts)
                {
                    account.ApplyInterest();
                }
            }
            
            // Списание платежей по кредитам
            foreach (var client in Clients)
            {
                foreach (var loan in client.Loans.ToList())
                {
                    if (client.Balance >= loan.MonthlyPayment)
                    {
                        bool paymentSuccess = loan.MakePayment();
                        if (paymentSuccess)
                        {
                            client.Balance -= loan.MonthlyPayment;
                            Capital += loan.MonthlyPayment;
                            
                            // Перемещение части прибыли в резервный фонд
                            decimal profit = loan.MonthlyPayment - (loan.Amount / loan.TermMonths);
                            ReserveFund += profit * 0.2m;
                            
                            if (loan.MonthsPaid == loan.TermMonths)
                            {
                                client.Loans.Remove(loan);
                                Console.WriteLine($"Кредит {loan.Id} клиента {client.Name} полностью погашен!");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Просрочка платежа по кредиту {loan.Id} клиента {client.Name}");
                        // Штрафные санкции
                        loan.InterestRate += 5; // Увеличение процентной ставки при просрочке
                    }
                }
            }
            
            // Обновление капитала банка
            Capital += ReserveFund * 0.01m; // 1% от резервного фонда как доход от инвестиций
        }
        
        public void ShowBankStatus()
        {
            Console.WriteLine($"\n=== Статус банка '{Name}' ===");
            Console.WriteLine($"Капитал банка: {Capital:C2}");
            Console.WriteLine($"Резервный фонд: {ReserveFund:C2}");
            Console.WriteLine($"Количество клиентов: {Clients.Count}");
            Console.WriteLine($"Общая сумма депозитов: {Clients.Sum(c => c.Accounts.Sum(a => a.Balance)):C2}");
            Console.WriteLine($"Общая сумма выданных кредитов: {Clients.Sum(c => c.Loans.Sum(l => l.Amount)):C2}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Console.WriteLine("=== БИЗНЕС-СИМУЛЯТОР БАНКОВСКОЙ СИСТЕМЫ ===\n");
            
            // Создание банка
            Bank bank = new Bank("Финансовый Банк", 1000000m);
            
            // Создание начальных клиентов
            var client1 = bank.AddClient("Иван Петров");
            var client2 = bank.AddClient("Анна Сидорова");
            var client3 = bank.AddClient("ООО 'Ромашка'");
            
            // Открытие счетов
            bank.OpenAccount(client1, 50000m, "Расчетный");
            bank.OpenAccount(client1, 100000m, "Депозит");
            bank.OpenAccount(client2, 75000m, "Накопительный");
            bank.OpenAccount(client3, 500000m, "Расчетный");
            
            // Выдача кредитов
            bank.GrantLoan(client1, 20000m, 12m, 24);
            bank.GrantLoan(client3, 100000m, 10m, 12);
            
            bool running = true;
            int currentMonth = 0;
            
            while (running)
            {
                Console.Clear();
                Console.WriteLine($"=== Месяц {currentMonth} ===");
                bank.ShowBankStatus();
                
                Console.WriteLine("\nМеню:");
                Console.WriteLine("1. Показать детальную информацию о клиентах");
                Console.WriteLine("2. Добавить нового клиента");
                Console.WriteLine("3. Открыть счет для клиента");
                Console.WriteLine("4. Выдать кредит");
                Console.WriteLine("5. Перейти к следующему месяцу");
                Console.WriteLine("6. Выполнить перевод между клиентами");
                Console.WriteLine("7. Выход");
                
                Console.Write("\nВыберите действие: ");
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        ShowClientsDetails(bank);
                        break;
                        
                    case "2":
                        Console.Write("Введите имя нового клиента: ");
                        string clientName = Console.ReadLine();
                        bank.AddClient(clientName);
                        Console.WriteLine($"Клиент {clientName} добавлен.");
                        break;
                        
                    case "3":
                        OpenAccountForClient(bank);
                        break;
                        
                    case "4":
                        GrantNewLoan(bank);
                        break;
                        
                    case "5":
                        bank.ProcessMonth();
                        currentMonth++;
                        Console.WriteLine($"Переход к месяцу {currentMonth} выполнен.");
                        break;
                        
                    case "6":
                        MakeTransfer(bank);
                        break;
                        
                    case "7":
                        running = false;
                        break;
                        
                    default:
                        Console.WriteLine("Неверный выбор.");
                        break;
                }
                
                if (choice != "7")
                {
                    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                }
            }
            
            Console.WriteLine("\nСимуляция завершена. Итоговый статус банка:");
            bank.ShowBankStatus();
        }
        
        static void ShowClientsDetails(Bank bank)
        {
            Console.WriteLine("\n=== Информация о клиентах ===");
            foreach (var client in bank.Clients)
            {
                Console.WriteLine($"\nКлиент #{client.Id}: {client.Name}");
                Console.WriteLine($"  Общий баланс: {client.Balance:C2}");
                
                Console.WriteLine("  Счета:");
                foreach (var account in client.Accounts)
                {
                    Console.WriteLine($"    {account.AccountNumber} ({account.AccountType}): {account.Balance:C2} " +
                                    $"(ставка: {account.InterestRate}%)");
                }
                
                Console.WriteLine("  Кредиты:");
                foreach (var loan in client.Loans)
                {
                    Console.WriteLine($"    Кредит #{loan.Id}: {loan.Amount:C2} под {loan.InterestRate}%");
                    Console.WriteLine($"      Ежемесячный платеж: {loan.MonthlyPayment:C2}");
                    Console.WriteLine($"      Остаток долга: {loan.GetRemainingBalance():C2}");
                    Console.WriteLine($"      Погашено: {loan.MonthsPaid}/{loan.TermMonths} месяцев");
                }
            }
        }
        
        static void OpenAccountForClient(Bank bank)
        {
            if (bank.Clients.Count == 0)
            {
                Console.WriteLine("Нет доступных клиентов.");
                return;
            }
            
            Console.WriteLine("Выберите клиента:");
            for (int i = 0; i < bank.Clients.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {bank.Clients[i].Name}");
            }
            
            if (int.TryParse(Console.ReadLine(), out int clientIndex) && 
                clientIndex >= 1 && clientIndex <= bank.Clients.Count)
            {
                var client = bank.Clients[clientIndex - 1];
                
                Console.WriteLine("Выберите тип счета:");
                Console.WriteLine("1. Расчетный (0.5%)");
                Console.WriteLine("2. Накопительный (3.5%)");
                Console.WriteLine("3. Депозит (5.0%)");
                
                string accountTypeChoice = Console.ReadLine();
                string accountType = accountTypeChoice switch
                {
                    "1" => "Расчетный",
                    "2" => "Накопительный",
                    "3" => "Депозит",
                    _ => "Расчетный"
                };
                
                Console.Write("Введите начальный взнос: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal initialDeposit))
                {
                    var account = bank.OpenAccount(client, initialDeposit, accountType);
                    Console.WriteLine($"Счет {account.AccountNumber} открыт для клиента {client.Name}");
                }
                else
                {
                    Console.WriteLine("Неверная сумма.");
                }
            }
            else
            {
                Console.WriteLine("Неверный выбор клиента.");
            }
        }
        
        static void GrantNewLoan(Bank bank)
        {
            if (bank.Clients.Count == 0)
            {
                Console.WriteLine("Нет доступных клиентов.");
                return;
            }
            
            Console.WriteLine("Выберите клиента для выдачи кредита:");
            for (int i = 0; i < bank.Clients.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {bank.Clients[i].Name} (баланс: {bank.Clients[i].Balance:C2})");
            }
            
            if (int.TryParse(Console.ReadLine(), out int clientIndex) && 
                clientIndex >= 1 && clientIndex <= bank.Clients.Count)
            {
                var client = bank.Clients[clientIndex - 1];
                
                Console.Write("Введите сумму кредита: ");
                if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
                {
                    Console.WriteLine("Неверная сумма.");
                    return;
                }
                
                Console.Write("Введите процентную ставку (годовых): ");
                if (!decimal.TryParse(Console.ReadLine(), out decimal interestRate))
                {
                    Console.WriteLine("Неверная ставка.");
                    return;
                }
                
                Console.Write("Введите срок кредита (в месяцах): ");
                if (!int.TryParse(Console.ReadLine(), out int termMonths))
                {
                    Console.WriteLine("Неверный срок.");
                    return;
                }
                
                var loan = bank.GrantLoan(client, amount, interestRate, termMonths);
                if (loan != null)
                {
                    Console.WriteLine($"Кредит выдан клиенту {client.Name}");
                    Console.WriteLine($"Ежемесячный платеж: {loan.MonthlyPayment:C2}");
                }
            }
            else
            {
                Console.WriteLine("Неверный выбор клиента.");
            }
        }
        
        static void MakeTransfer(Bank bank)
        {
            if (bank.Clients.Count < 2)
            {
                Console.WriteLine("Недостаточно клиентов для перевода.");
                return;
            }
            
            Console.WriteLine("Выберите отправителя:");
            for (int i = 0; i < bank.Clients.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {bank.Clients[i].Name} (баланс: {bank.Clients[i].Balance:C2})");
            }
            
            if (!int.TryParse(Console.ReadLine(), out int senderIndex) || 
                senderIndex < 1 || senderIndex > bank.Clients.Count)
            {
                Console.WriteLine("Неверный выбор отправителя.");
                return;
            }
            
            Console.WriteLine("Выберите получателя:");
            for (int i = 0; i < bank.Clients.Count; i++)
            {
                if (i != senderIndex - 1)
                {
                    Console.WriteLine($"{i + 1}. {bank.Clients[i].Name}");
                }
            }
            
            if (!int.TryParse(Console.ReadLine(), out int receiverIndex) || 
                receiverIndex < 1 || receiverIndex > bank.Clients.Count ||
                receiverIndex == senderIndex)
            {
                Console.WriteLine("Неверный выбор получателя.");
                return;
            }
            
            var sender = bank.Clients[senderIndex - 1];
            var receiver = bank.Clients[receiverIndex - 1];
            
            Console.Write("Введите сумму перевода: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0)
            {
                Console.WriteLine("Неверная сумма.");
                return;
            }
            
            if (sender.Balance >= amount)
            {
                sender.Balance -= amount;
                receiver.Balance += amount;
                Console.WriteLine($"Перевод {amount:C2} от {sender.Name} к {receiver.Name} выполнен успешно.");
            }
            else
            {
                Console.WriteLine("Недостаточно средств у отправителя.");
            }
        }
    }
}
