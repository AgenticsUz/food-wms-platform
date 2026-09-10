using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Seeding;

// Kontragentlar, agentlar, transferlar, qarz, to'lov va komissiya — TransferService/FinanceService/AgentService qoidalari.
internal sealed partial class DemoData
{
    private readonly List<TransferItem> _transferItems = [];
    private readonly Dictionary<Guid, Debt> _debts = [];
    private readonly Dictionary<Guid, CommissionRecord> _commissionBySale = [];
    private readonly List<(PaymentHistory Payment, PaymentDirection Direction)> _payments = [];

    private Counterparty _nemat = null!;
    private Counterparty _baraka = null!;
    private Counterparty _sharq = null!;
    private Counterparty _korzinka = null!;
    private Counterparty _makro = null!;
    private Counterparty _plov = null!;
    private Agent _anvar = null!;
    private Agent _sevara = null!;

    private void BuildPartners()
    {
        // Agentlar eski demo'da yo'q edi — AGENTS moduli bo'sh ekran bilan ochilmasin. Portal
        // login'i yo'q (D8), WMS profiliga ham bog'lanmagan.
        _anvar = Add(new Agent { Name = "Anvar Qosimov", Phone = "+998901234501", CommissionPercent = 3m });
        _sevara = Add(new Agent { Name = "Sevara Nurmatova", Phone = "+998901234502", CommissionPercent = 2.5m });

        // Telefonlar E.164 da (eski demo'da «998901111111» — PhoneHelper normalizatsiyasidan oldingi shakl).
        Counterparty Partner(string name, CounterpartyType type, string phone, string inn, string address, Agent? agent = null) =>
            Add(new Counterparty { Name = name, Type = type, Phone = phone, Inn = inn, Address = address, AgentId = agent?.Id });

        _nemat = Partner("Nemat Agro", CounterpartyType.Supplier, "+998901111111", "301245781", "Toshkent viloyati, Zangiota tumani");
        _baraka = Partner("Baraka Sut zavodi", CounterpartyType.Supplier, "+998902222222", "302356892", "Samarqand, Sanoat zonasi 4");
        _sharq = Partner("Sharq Savdo", CounterpartyType.Supplier, "+998903333333", "303467903", "Toshkent, Sergeli tumani, Yangi Sergeli 12");

        _korzinka = Partner("Supermarket Korzinka", CounterpartyType.Client, "+998904444444", "304578014", "Toshkent, Yunusobod tumani, Amir Temur ko'chasi 107", _anvar);
        _makro = Partner("Do'kon Makro", CounterpartyType.Client, "+998905555555", "305689125", "Toshkent, Chilonzor tumani, Bunyodkor shoh ko'chasi 5", _sevara);
        _plov = Partner("Restoran Plov Markazi", CounterpartyType.Client, "+998906666666", "306790236", "Toshkent, Mirobod tumani, Oybek ko'chasi 18");
    }

    /// <summary>Qarz — FAQAT shu yerdan (musbat: ular bizga qarz; manfiy: biz ularga). Qator birinchi harakatda ochiladi (servisdagidek).</summary>
    private void ChangeDebt(Counterparty counterparty, decimal delta, DateTime at)
    {
        if (!_debts.TryGetValue(counterparty.Id, out Debt? debt))
        {
            debt = Add(new Debt { CounterpartyId = counterparty.Id, Amount = 0 }, at);
            _debts[counterparty.Id] = debt;
        }

        debt.Amount += delta;
    }

    private TransferItem Item(Transfer transfer, Product product, Batch? batch, decimal quantity, decimal unitPrice, DateTime at)
    {
        TransferItem item = Add(
            new TransferItem { TransferId = transfer.Id, ProductId = product.Id, BatchId = batch?.Id, Quantity = quantity, UnitPrice = unitPrice },
            at);
        _transferItems.Add(item);
        return item;
    }

    /// <summary>Tasdiqlangan kirim: yangi partiya + qoldiq, biz yetkazib beruvchiga qarzdor bo'lamiz.</summary>
    private Transfer Incoming(Counterparty supplier, Product product, decimal quantity, decimal unitPrice, DateTime at, Location location)
    {
        Batch batch = NewBatch(product, "LOT", at, ExpiryFrom(product, at), quantity, at);
        AddStock(_rawWarehouse, location, product, batch, quantity, at);

        Transfer transfer = Add(
            new Transfer
            {
                Type = TransferType.Incoming,
                ToWarehouseId = _rawWarehouse.Id,
                CounterpartyId = supplier.Id,
                CreatedByUserId = _people.Manager.Id,
                Status = TransferStatus.Confirmed,
                ConfirmedAt = at,
            },
            at);
        Item(transfer, product, batch, quantity, unitPrice, at);

        ChangeDebt(supplier, -(quantity * unitPrice), at);
        return transfer;
    }

    /// <summary>Tasdiq kutayotgan kirim: partiya tasdiqda ochiladi — hozircha zaxira ham, qarz ham o'zgarmaydi.</summary>
    private void PendingIncoming(Counterparty supplier, Product product, decimal quantity, decimal unitPrice, DateTime at)
    {
        Transfer transfer = Add(
            new Transfer
            {
                Type = TransferType.Incoming,
                ToWarehouseId = _rawWarehouse.Id,
                CounterpartyId = supplier.Id,
                CreatedByUserId = _people.Employee.Id,
                Status = TransferStatus.Pending,
                Note = "Yetkazib beruvchi yuk xatini yubordi — omborda qabul qilinishi kutilmoqda",
            },
            at);
        Item(transfer, product, null, quantity, unitPrice, at);
    }

    private Transfer Sale(Counterparty client, Product product, decimal quantity, decimal unitPrice, DateTime at, Agent? agent)
    {
        Sale(client, product, quantity, unitPrice, at, agent, out Transfer transfer);
        return transfer;
    }

    /// <summary>
    /// Tasdiqlangan sotuv: FEFO bilan tayyor ombordan (har partiya — alohida qator), mijoz qarzi
    /// oshadi, agent orqali bo'lsa «kutilmoqda» komissiyasi yoziladi.
    /// </summary>
    private void Sale(Counterparty client, Product product, decimal quantity, decimal unitPrice, DateTime at, Agent? agent, out Transfer transfer)
    {
        transfer = Add(
            new Transfer
            {
                Type = TransferType.Outgoing,
                FromWarehouseId = _finishedWarehouse.Id,
                CounterpartyId = client.Id,
                AgentId = agent?.Id,
                CreatedByUserId = _people.Manager.Id,
                Status = TransferStatus.Confirmed,
                ConfirmedAt = at,
            },
            at);

        foreach ((WarehouseStock stock, decimal taken) in Take(product, quantity, _finishedWarehouse))
        {
            Item(transfer, product, _batches[stock.BatchId], taken, unitPrice, at);
        }

        decimal amount = quantity * unitPrice;
        ChangeDebt(client, amount, at);

        if (agent is not null)
        {
            _commissionBySale[transfer.Id] = Add(
                new CommissionRecord
                {
                    AgentId = agent.Id,
                    TransferId = transfer.Id,
                    SaleAmount = amount,
                    CommissionPercent = agent.CommissionPercent,
                    CommissionAmount = Math.Round(amount * agent.CommissionPercent / 100m, 2),
                    Status = CommissionStatus.Pending,
                },
                at);
        }
    }

    /// <summary>
    /// Qaytarish: yangi partiya ASL partiyaning sanalari bilan (FEFO halol qolsin), mijoz qarzi
    /// kamayadi, komissiya mutanosib kamayadi (<c>AdjustCommissionForReturn</c>).
    /// </summary>
    private void Return(Transfer sale, Product product, decimal quantity, ReturnReason reason, DateTime at, string note)
    {
        TransferItem soldItem = _transferItems.First(i => i.TransferId == sale.Id && i.ProductId == product.Id && i.BatchId is not null);
        Batch original = _batches[soldItem.BatchId!.Value];
        Counterparty client = Created<Counterparty>().First(c => c.Id == sale.CounterpartyId);

        Batch batch = NewBatch(product, "LOT-RET", original.ManufacturedDate, original.ExpiryDate, quantity, at, $"Qaytarildi: {original.LotNumber}");
        AddStock(_finishedWarehouse, _locB2, product, batch, quantity, at);

        Transfer transfer = Add(
            new Transfer
            {
                Type = TransferType.Return,
                ToWarehouseId = _finishedWarehouse.Id,
                CounterpartyId = client.Id,
                CreatedByUserId = _people.Manager.Id,
                Status = TransferStatus.Confirmed,
                ConfirmedAt = at,
                ReturnReason = reason,
                OriginalTransferId = sale.Id,
                Note = note,
            },
            at);
        Item(transfer, product, batch, quantity, soldItem.UnitPrice, at);

        decimal amount = quantity * soldItem.UnitPrice;
        ChangeDebt(client, -amount, at);

        if (_commissionBySale.TryGetValue(sale.Id, out CommissionRecord? commission) && commission.Status != CommissionStatus.Cancelled)
        {
            // Ssenariyda qaytarish komissiya to'lanishidan OLDIN — servisdagi joyida kamaytirish yo'li.
            // To'langan komissiyada servis manfiy yozuv ochadi; demo'da u holat yo'q, bo'lsa — xato.
            if (commission.IsPaid)
            {
                throw new InvalidOperationException("Demo ssenariysi: to'langan komissiyali sotuv qaytarilmaydi.");
            }

            decimal reduction = Math.Min(commission.CommissionAmount, Math.Round(amount * commission.CommissionPercent / 100m, 2));
            commission.SaleAmount -= amount;
            commission.CommissionAmount -= reduction;
            if (commission.CommissionAmount <= 0)
            {
                commission.Status = CommissionStatus.Cancelled;
            }
        }
    }

    /// <summary>
    /// To'lov: to'lov tarixi + qarz (<c>FinanceService.CreatePaymentAsync</c>) va kassa daftaridagi
    /// kirim/chiqim yozuvi (eski demo'dagi «Korzinka to'lovi» tranzaksiyalari shu).
    /// </summary>
    private void Payment(Counterparty counterparty, PaymentDirection direction, decimal amount, PaymentMethod method, DateTime at, Transfer? forTransfer = null)
    {
        PaymentHistory payment = Add(
            new PaymentHistory
            {
                CounterpartyId = counterparty.Id,
                TransferId = forTransfer?.Id,
                Amount = amount,
                Method = method,
                PaidAt = at,
                RecordedByUserId = _people.Admin.Id,
            },
            at);
        _payments.Add((payment, direction));

        ChangeDebt(counterparty, direction == PaymentDirection.In ? -amount : amount, at);

        Add(
            new Transaction
            {
                Type = direction == PaymentDirection.In ? TransactionType.Income : TransactionType.Expense,
                CounterpartyId = counterparty.Id,
                TransferId = forTransfer?.Id,
                Amount = amount,
                Description = direction == PaymentDirection.In ? $"{counterparty.Name} to'lovi" : $"{counterparty.Name}ga to'lov",
                Date = at,
                RecordedByUserId = _people.Admin.Id,
            },
            at);

        // Mijoz sotuvni TO'LIQ to'ladi — menejer komissiyani «tasdiqlangan» qiladi (servisda bu
        // qo'lda: UpdateCommissionStatusAsync). Qisman to'lovda komissiya kutilmoqda qoladi.
        if (forTransfer is not null
            && _commissionBySale.TryGetValue(forTransfer.Id, out CommissionRecord? commission)
            && commission.Status == CommissionStatus.Pending
            && amount >= commission.SaleAmount)
        {
            commission.Status = CommissionStatus.Confirmed;
        }
    }

    private void Expense(string description, decimal amount, DateTime at) =>
        Add(new Transaction { Type = TransactionType.Expense, Amount = amount, Description = description, Date = at, RecordedByUserId = _people.Admin.Id }, at);

    /// <summary>Agentga tasdiqlangan komissiyalarini to'lash + xarajat yozuvi (<c>PayCommissionAsync</c>, <c>RecordAsExpense</c>).</summary>
    private void PayCommissions(Agent agent, DateTime at)
    {
        List<CommissionRecord> due = [.. _commissionBySale.Values.Where(c => c.AgentId == agent.Id && !c.IsPaid && c.Status == CommissionStatus.Confirmed)];
        if (due.Count == 0)
        {
            return;
        }

        foreach (CommissionRecord record in due)
        {
            record.IsPaid = true;
            record.PaidAt = at;
        }

        Expense($"Agent komissiya to'lovi: {agent.Name}", due.Sum(c => c.CommissionAmount), at);
    }
}
