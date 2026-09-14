namespace WMS.Application.Common.Localization;

/// <summary>
/// Every user-facing string the API returns, in Uzbek and Russian, keyed by its English text.
///
/// Keying by the English message rather than by an invented code means the ~100 existing
/// `throw new AppException("Tenant not found")` sites needed no edit: the thrown text is the
/// lookup key, and an entry that is missing simply comes back in English. Adding a language
/// is one more column; adding a message is one more row.
///
/// Kept in code on purpose — these strings ship with the build, so they can never be out of
/// step with the code that throws them, and no database round-trip stands between a customer
/// and an error message.
/// </summary>
public static class Translations
{
    // [English] = { uz, ru }
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.Ordinal)
    {
        // ── F6: Postgres/Identity'ga o'tish bilan paydo bo'lgan xabarlar ──
        ["The record was changed by someone else. Reload and try again."] = [
            "Bu yozuvni hozirgina boshqa foydalanuvchi o'zgartirdi. Sahifani yangilab, qayta urinib ko'ring.",
            "Эту запись только что изменил другой пользователь. Обновите страницу и повторите попытку."],
        ["The request body is malformed"] = [
            "So'rov ma'lumoti noto'g'ri shaklda.",
            "Тело запроса имеет неверный формат."],
        ["Tenant is required"] = [
            "Tashkilot tanlanmagan. Tizimga qayta kiring.",
            "Организация не выбрана. Войдите в систему заново."],
        ["Your profile is not available in this organization"] = [
            "Bu tashkilotda profilingiz yo'q.",
            "В этой организации у вас нет профиля."],
        ["Insufficient stock for input '{0}' (short by {1})"] = [
            "'{0}' xomashyosi yetarli emas ({1} yetishmaydi)",
            "Недостаточно сырья '{0}' (не хватает {1})"],
        // ── Bildirishnomalar (TG4): sarlavha va matn kalitlari — NotificationMessages ──
        ["Transfer Confirmed"] = ["Transfer tasdiqlandi", "Перемещение подтверждено"],
        ["Transfer Rejected"] = ["Transfer rad etildi", "Перемещение отклонено"],
        ["Return Received"] = ["Qaytarish qabul qilindi", "Возврат принят"],
        ["Low Stock Alert"] = ["Zaxira kam", "Мало на складе"],
        ["Batch Expired"] = ["Partiya muddati o'tdi", "Срок партии истёк"],
        ["Batch Expiring"] = ["Partiya muddati tugayapti", "Срок партии истекает"],
        ["Production Started"] = ["Ishlab chiqarish boshlandi", "Производство начато"],
        ["Production Completed"] = ["Ishlab chiqarish yakunlandi", "Производство завершено"],
        ["Incoming from {0} ({1}) confirmed. Amount: {2}"] = [
            "{0} dan kirim ({1}) tasdiqlandi. Summa: {2}",
            "Приход от {0} ({1}) подтверждён. Сумма: {2}"],
        ["Sale to {0} ({1}) confirmed. Amount: {2}"] = [
            "{0} ga sotuv ({1}) tasdiqlandi. Summa: {2}",
            "Продажа {0} ({1}) подтверждена. Сумма: {2}"],
        ["Internal transfer {0} → {1} ({2}) confirmed"] = [
            "Ichki ko'chirish {0} → {1} ({2}) tasdiqlandi",
            "Внутреннее перемещение {0} → {1} ({2}) подтверждено"],
        ["Return from {0} ({1}) received. Amount: {2}"] = [
            "{0} dan qaytarish ({1}) qabul qilindi. Summa: {2}",
            "Возврат от {0} ({1}) принят. Сумма: {2}"],
        ["Transfer {0} ({1}) rejected"] = ["Transfer {0} ({1}) rad etildi", "Перемещение {0} ({1}) отклонено"],
        ["{0}: {1} {2} left, minimum {3}"] = ["{0}: {1} {2} qoldi, minimum {3}", "{0}: осталось {1} {2}, минимум {3}"],
        ["Batch {0} of {1} has expired"] = ["{1} — {0} partiyasi muddati o'tdi", "{1} — срок партии {0} истёк"],
        ["Batch {0} of {1} expires on {2}"] = ["{1} — {0} partiyasi muddati {2} da tugaydi", "{1} — срок партии {0} истекает {2}"],
        ["Order {0} ({1}) started. Responsible: {2}"] = [
            "{0} buyurtmasi ({1}) boshlandi. Javobgar: {2}",
            "Заказ {0} ({1}) начат. Ответственный: {2}"],
        ["Order {0} ({1}) completed. Output: {2} {3}"] = [
            "{0} buyurtmasi ({1}) yakunlandi. Chiqish: {2} {3}",
            "Заказ {0} ({1}) завершён. Выпуск: {2} {3}"],
        ["Open"] = ["Ochish", "Открыть"],
        ["Transfer Awaiting Confirmation"] = ["Transfer tasdiq kutmoqda", "Перемещение ожидает подтверждения"],
        ["Production Order Created"] = ["Ishlab chiqarish buyurtmasi yaratildi", "Создан производственный заказ"],
        ["Incoming from {0} ({1}), amount {2}, created by {3}"] = [
            "{0} dan kirim ({1}), summa {2}, yaratdi: {3}",
            "Приход от {0} ({1}), сумма {2}, создал: {3}"],
        ["Sale to {0} ({1}), amount {2}, created by {3}"] = [
            "{0} ga sotuv ({1}), summa {2}, yaratdi: {3}",
            "Продажа {0} ({1}), сумма {2}, создал: {3}"],
        ["Internal transfer {0} → {1} ({2}), created by {3}"] = [
            "Ichki ko'chirish {0} → {1} ({2}), yaratdi: {3}",
            "Внутреннее перемещение {0} → {1} ({2}), создал: {3}"],
        ["Return from {0} ({1}), amount {2}, created by {3}"] = [
            "{0} dan qaytarish ({1}), summa {2}, yaratdi: {3}",
            "Возврат от {0} ({1}), сумма {2}, создал: {3}"],
        ["Order {0} ({1}) is planned. Quantity: {2} {3}"] = [
            "{0} buyurtmasi ({1}) rejalashtirildi. Miqdor: {2} {3}",
            "Заказ {0} ({1}) запланирован. Количество: {2} {3}"],

        // ── So'rov buyruqlari (TG10) ──
        ["Which organization?"] = ["Qaysi tashkilot?", "Какая организация?"],
        ["Type the product name: /qoldiq plombir"] = ["Mahsulot nomini yozing: /qoldiq plombir", "Введите название товара: /qoldiq пломбир"],
        ["Stock"] = ["Qoldiq", "Остаток"],
        ["Nothing found for \"{0}\"."] = ["«{0}» bo'yicha hech narsa topilmadi.", "По запросу «{0}» ничего не найдено."],
        ["Batches expiring within {0} days"] = ["{0} kun ichida muddati tugaydigan partiyalar", "Партии с истекающим сроком ({0} дн.)"],
        ["No batches expire within {0} days."] = ["{0} kun ichida muddati tugaydigan partiya yo'q.", "Партий с истекающим сроком ({0} дн.) нет."],
        ["Receivables"] = ["Qarzdorlar", "Должники"],
        ["No outstanding receivables."] = ["Qarzdorlik yo'q.", "Задолженности нет."],
        ["Today"] = ["Bugun", "Сегодня"],
        ["Transfers created today: {0}"] = ["Bugun yaratilgan transferlar: {0}", "Перемещений создано сегодня: {0}"],
        ["No transfers are waiting for confirmation."] = ["Tasdiq kutayotgan transfer yo'q.", "Перемещений в ожидании подтверждения нет."],
        ["… and {0} more"] = ["… va yana {0}", "… и ещё {0}"],

        // ── Xodim buyruqlari va hisobot fayllari (TG14/TG15) ──
        ["This feature is not included in your plan."] = ["Bu imkoniyat tarifingizga kirmaydi.", "Эта возможность не входит в ваш тариф."],
        ["You already checked in at {0}."] = ["Siz allaqachon keldingiz ({0}).", "Вы уже отметились ({0})."],
        ["You have not checked in yet — send /keldim first."] = ["Hali kelmagansiz — avval /keldim.", "Вы ещё не отметились — сначала /keldim."],
        ["No shifts are set up yet."] = ["Smenalar hali sozlanmagan.", "Смены ещё не настроены."],
        ["Which shift?"] = ["Qaysi smena?", "Какая смена?"],
        ["✅ Checked in: {0}, {1}."] = ["✅ Keldingiz: {0}, {1}.", "✅ Отмечен приход: {0}, {1}."],
        ["👋 Checked out at {0}. Worked: {1}."] = ["👋 Ketdingiz: {0}. Ishlagan vaqt: {1}.", "👋 Отмечен уход: {0}. Отработано: {1}."],
        ["No shift plans for today."] = ["Bugunga smena rejasi yo'q.", "На сегодня плана смен нет."],
        ["Shift plans — {0}"] = ["Smena rejalari — {0}", "План смен — {0}"],
        ["No efficiency data for this week yet."] = ["Bu hafta samaradorlik ma'lumoti hali yo'q.", "Данных об эффективности за эту неделю пока нет."],
        ["Efficiency this week"] = ["Shu hafta samaradorlik", "Эффективность за неделю"],
        ["Which report?"] = ["Qaysi hisobot?", "Какой отчёт?"],
        ["📦 Stock (Excel)"] = ["📦 Zaxira (Excel)", "📦 Остатки (Excel)"],
        ["🔄 Transfers — this month (Excel)"] = ["🔄 Transferlar — shu oy (Excel)", "🔄 Перемещения — этот месяц (Excel)"],
        ["💰 Finance — this month (Excel)"] = ["💰 Moliya — shu oy (Excel)", "💰 Финансы — этот месяц (Excel)"],
        ["🤝 Counterparties (Excel)"] = ["🤝 Kontragentlar (Excel)", "🤝 Контрагенты (Excel)"],
        ["The report is too large for Telegram — download it from the web app."] = [
            "Hisobot Telegram uchun juda katta — web ilovadan yuklab oling.",
            "Отчёт слишком большой для Telegram — скачайте его в веб-приложении."],

        // ── Haydovchi va mijoz (TG12/TG13) ──
        ["Delivery Stop Failed"] = ["Yetkazish amalga oshmadi", "Доставка не выполнена"],
        ["{0} could not deliver to {1} ({2})"] = ["{0} {1} ga yetkaza olmadi ({2})", "{0} не смог(ла) доставить {1} ({2})"],
        ["Your order from {0} ({1}) is confirmed. Amount: {2}"] = [
            "{0} dan buyurtmangiz ({1}) tasdiqlandi. Summa: {2}",
            "Ваш заказ от {0} ({1}) подтверждён. Сумма: {2}"],
        ["Your order from {0} is on the way today ({1})."] = ["{0} dan buyurtmangiz bugun yo'lda ({1}).", "Ваш заказ от {0} сегодня в пути ({1})."],
        ["Your order from {0} has been delivered. Thank you!"] = ["{0} dan buyurtmangiz yetkazildi. Rahmat!", "Ваш заказ от {0} доставлен. Спасибо!"],
        ["{0}: payment of {1} received. Your balance: {2}"] = ["{0}: {1} to'lov qabul qilindi. Balansingiz: {2}", "{0}: получена оплата {1}. Ваш баланс: {2}"],
        ["Reminder from {0}: your outstanding balance is {1}. Please settle it."] = [
            "{0} eslatmasi: qarzingiz {1}. Iltimos, to'lang.",
            "Напоминание от {0}: ваша задолженность {1}. Пожалуйста, оплатите."],
        ["Your balance with {0}: {1}"] = ["{0} oldidagi qarzingiz: {1}", "Ваша задолженность перед {0}: {1}"],
        ["You have no outstanding balance with {0}."] = ["{0} oldida qarzingiz yo'q.", "У вас нет задолженности перед {0}."],
        ["Route — {0}"] = ["Marshrut — {0}", "Маршрут — {0}"],
        ["📄 Waybill (PDF)"] = ["📄 Yuk xati (PDF)", "📄 Накладная (PDF)"],
        ["No deliveries planned for you today."] = ["Bugun sizga yetkazish rejalashtirilmagan.", "На сегодня доставок для вас не запланировано."],
        ["✅ Delivered: {0}"] = ["✅ Yetkazildi: {0}", "✅ Доставлено: {0}"],
        ["❌ Not delivered: {0}"] = ["❌ Yetkazilmadi: {0}", "❌ Не доставлено: {0}"],
        ["Recent payments:"] = ["Oxirgi to'lovlar:", "Последние оплаты:"],
        ["✅ Connected as a driver of <b>{0}</b>. Your routes will arrive here; /marshrut — today's route."] = [
            "✅ <b>{0}</b> haydovchisi sifatida ulandingiz. Marshrutlar shu yerga keladi; /marshrut — bugungi marshrut.",
            "✅ Вы подключены как водитель <b>{0}</b>. Маршруты будут приходить сюда; /marshrut — маршрут на сегодня."],
        ["✅ Connected to <b>{0}</b>. You will receive order and delivery updates here; /qarzim — your balance."] = [
            "✅ <b>{0}</b> ga ulandingiz. Buyurtma va yetkazish xabarlari shu yerga keladi; /qarzim — balansingiz.",
            "✅ Вы подключены к <b>{0}</b>. Уведомления о заказах и доставке будут приходить сюда; /qarzim — ваш баланс."],
        ["Telegram is not connected"] = ["Telegram ulanmagan", "Telegram не подключён"],
        ["Debt reminder interval must be between 0 and 90 days"] = [
            "Qarz eslatmasi oralig'i 0–90 kun bo'lishi kerak", "Интервал напоминания о долге должен быть от 0 до 90 дней"],

        // ── Guruh chati (TG16) ──
        ["Only an administrator with a connected profile can do this (WMS → Settings → Profile → Connect Telegram)."] = [
            "Buni faqat profili ulangan administrator qila oladi (WMS → Sozlamalar → Profil → Telegram'ga ulash).",
            "Это может сделать только администратор с подключённым профилем (WMS → Настройки → Профиль → Подключить Telegram)."],
        ["✅ This group is now connected to <b>{0}</b>. Notifications will arrive here. /sozlash — choose types, /uzish — disconnect."] = [
            "✅ Guruh <b>{0}</b> ga ulandi. Bildirishnomalar shu yerga keladi. /sozlash — turlarni tanlash, /uzish — uzish.",
            "✅ Группа подключена к <b>{0}</b>. Уведомления будут приходить сюда. /sozlash — выбрать типы, /uzish — отключить."],
        ["This group has been disconnected."] = ["Guruh uzildi.", "Группа отключена."],
        ["This group is not connected. An administrator can connect it with /ulash."] = [
            "Bu guruh ulanmagan. Administrator /ulash bilan ulaydi.",
            "Эта группа не подключена. Администратор может подключить её командой /ulash."],
        ["Which notifications should come to this group?"] = ["Guruhga qaysi bildirishnomalar kelsin?", "Какие уведомления присылать в группу?"],
        ["Connect your own profile first: WMS → Settings → Profile → Connect Telegram."] = [
            "Avval o'z profilingizni ulang: WMS → Sozlamalar → Profil → Telegram'ga ulash.",
            "Сначала подключите свой профиль: WMS → Настройки → Профиль → Подключить Telegram."],
        ["Stock and batches"] = ["Zaxira va partiyalar", "Склад и партии"],
        ["Transfers"] = ["Transferlar", "Перемещения"],
        ["Production"] = ["Ishlab chiqarish", "Производство"],

        // ── Kunlik xulosa (TG11) ──
        ["Daily summary — {0}"] = ["Kunlik xulosa — {0}", "Сводка за день — {0}"],
        ["Transfers awaiting confirmation: {0}"] = ["Tasdiq kutayotgan transferlar: {0}", "Перемещений в ожидании подтверждения: {0}"],
        ["Active production orders: {0}"] = ["Faol ishlab chiqarish buyurtmalari: {0}", "Активных производственных заказов: {0}"],
        ["Low-stock products: {0}"] = ["Zaxirasi kam mahsulotlar: {0}", "Товаров с низким запасом: {0}"],
        ["Batches expiring within {0} days: {1}"] = ["{0} kun ichida muddati tugaydigan partiyalar: {1}", "Партий с истекающим сроком ({0} дн.): {1}"],
        ["Total receivables: {0}"] = ["Jami qarzdorlik: {0}", "Общая дебиторская задолженность: {0}"],
        ["Largest debtors:"] = ["Eng katta qarzdorlar:", "Крупнейшие должники:"],

        // ── Telegram tugmalari va javoblari (TG9) ──
        ["✅ Confirm"] = ["✅ Tasdiqlash", "✅ Подтвердить"],
        ["❌ Reject"] = ["❌ Rad etish", "❌ Отклонить"],
        ["▶️ Start"] = ["▶️ Boshlash", "▶️ Начать"],
        ["✅ Confirmed by {0}, {1}"] = ["✅ Tasdiqladi: {0}, {1}", "✅ Подтвердил(а): {0}, {1}"],
        ["❌ Rejected by {0}, {1}"] = ["❌ Rad etdi: {0}, {1}", "❌ Отклонил(а): {0}, {1}"],
        ["▶️ Started by {0}, {1}"] = ["▶️ Boshladi: {0}, {1}", "▶️ Начал(а): {0}, {1}"],
        ["Already processed by someone else."] = ["Allaqachon boshqa odam bajargan.", "Уже выполнено кем-то другим."],
        ["You do not have permission for this action."] = ["Bu amal uchun ruxsatingiz yo'q.", "У вас нет права на это действие."],
        ["This button has expired."] = ["Bu tugma eskirgan.", "Эта кнопка устарела."],
        ["Only pending transfers can be confirmed"] = [
            "Faqat kutilayotgan transferni tasdiqlash mumkin", "Подтвердить можно только ожидающее перемещение"],
        ["Only pending transfers can be rejected"] = [
            "Faqat kutilayotgan transferni rad etish mumkin", "Отклонить можно только ожидающее перемещение"],
        ["Only draft orders can be started"] = [
            "Faqat qoralama buyurtmani boshlash mumkin", "Начать можно только черновой заказ"],
        ["Subscription Expiring"] = ["Obuna muddati tugayapti", "Срок подписки истекает"],
        ["Account Suspended"] = ["Hisob to'xtatildi", "Аккаунт приостановлен"],
        ["Plan Limit"] = ["Tarif limiti", "Лимит тарифа"],
        ["Your trial ends on {0}. Choose a plan to continue."] = [
            "Sinov muddati {0} da tugaydi. Davom etish uchun tarif tanlang.",
            "Пробный период заканчивается {0}. Выберите тариф, чтобы продолжить."],
        ["Your paid period ends on {0}. Contact us to renew."] = [
            "To'lov muddati {0} da tugaydi. Uzaytirish uchun biz bilan bog'laning.",
            "Оплаченный период заканчивается {0}. Свяжитесь с нами для продления."],
        ["Your account is suspended for non-payment. Contact us to restore access."] = [
            "Hisobingiz to'lov qilinmagani uchun to'xtatildi. Tiklash uchun biz bilan bog'laning.",
            "Аккаунт приостановлен из-за неоплаты. Свяжитесь с нами для восстановления доступа."],

        // ── Telegram bot buyruqlari (TG6) ──
        ["You are not connected to any organization. Open your WMS profile → Connect Telegram."] = [
            "Hech qaysi tashkilotga ulanmagansiz. WMS profilingiz → Telegram'ga ulash.",
            "Вы не подключены ни к одной организации. Профиль WMS → Подключить Telegram."],
        ["Connected organizations:"] = ["Ulangan tashkilotlar:", "Подключённые организации:"],
        ["{0} notification type(s) muted"] = ["{0} tur o'chirilgan", "отключено типов: {0}"],
        ["Disconnected from all organizations. To reconnect, open your WMS profile."] = [
            "Barcha tashkilotlardan uzildingiz. Qayta ulash — WMS profilingizdan.",
            "Отключено от всех организаций. Чтобы подключиться снова, откройте профиль WMS."],
        // Qavslar KVADRAT: javob HTML rejimida ketadi, `<nom>` Telegram uchun noma'lum teg (HTTP 400).
        ["Commands: /bugun — today, /kutilmoqda — pending transfers, /qoldiq [name] — stock, /muddat — expiring batches, /qarz — receivables, /hisobot — Excel report, /keldim /ketdim — attendance, /smena — shift plans, /kpi — efficiency, /status — where you are connected, /stop — disconnect."] = [
            "Buyruqlar: /bugun — bugungi holat, /kutilmoqda — tasdiq kutayotgan transferlar, /qoldiq [nom] — qoldiq, /muddat — muddati yaqin partiyalar, /qarz — qarzdorlar, /hisobot — Excel hisobot, /keldim /ketdim — davomat, /smena — smena rejasi, /kpi — samaradorlik, /status — qayerga ulangansiz, /stop — uzish.",
            "Команды: /bugun — сводка, /kutilmoqda — перемещения в ожидании, /qoldiq [название] — остаток, /muddat — партии с истекающим сроком, /qarz — должники, /hisobot — отчёт Excel, /keldim /ketdim — посещаемость, /smena — план смен, /kpi — эффективность, /status — где вы подключены, /stop — отключить."],

        // ── Telegram bot (TG1): API xabarlari va botning o'z javoblari (HTML) ──
        ["Telegram bot is not configured"] = [
            "Telegram bot hali sozlanmagan",
            "Telegram-бот ещё не настроен"],
        ["Telegram disconnected"] = ["Telegram uzildi", "Telegram отключён"],
        ["✅ Connected to <b>{0}</b>. Notifications will arrive here."] = [
            "✅ <b>{0}</b> ga ulandi. Bildirishnomalar shu yerga keladi.",
            "✅ Подключено к <b>{0}</b>. Уведомления будут приходить сюда."],
        ["This link has expired or was already used. Open your WMS profile and request a new one."] = [
            "Havola eskirgan yoki allaqachon ishlatilgan. WMS profilingizdan yangisini oling.",
            "Ссылка устарела или уже использована. Получите новую в профиле WMS."],
        ["To receive notifications, connect this chat from WMS: Settings → Profile → Connect Telegram."] = [
            "Bildirishnomalarni olish uchun WMS'dan ulang: Sozlamalar → Profil → Telegram'ga ulash.",
            "Чтобы получать уведомления, подключите чат из WMS: Настройки → Профиль → Подключить Telegram."],

        // Transfer/moliya (W1·2): ilgari interpolyatsiya bilan qurilardi va tarjima qilinmasdi.
        // Raqamsiz xabar («qoldiq yo'q») menejerga hech narsa aytmasdi: u ekranda
        // qoldiqni ko'rib turardi va nima yetmaganini bilmasdi.
        ["Insufficient available stock for product {0}: need {1:N2}, available {2:N2}"] = [
            "'{0}' mahsulotidan omborda yetarli qoldiq yo'q: kerak {1:N2}, mavjud {2:N2}",
            "Недостаточно доступного остатка товара '{0}': нужно {1:N2}, доступно {2:N2}"],
        ["Product {0} was not part of the original sale"] = [
            "'{0}' mahsuloti asl sotuvda bo'lmagan",
            "Товар '{0}' не входил в исходную продажу"],
        ["Return quantity for product {0} exceeds the remaining sold quantity ({1:N2})"] = [
            "'{0}' bo'yicha qaytarish miqdori qolgan sotilgan miqdordan ({1:N2}) ko'p",
            "Количество возврата по товару '{0}' превышает оставшееся проданное количество ({1:N2})"],
        ["{0} transfer must have a destination warehouse"] = [
            "{0} transferida qabul qiluvchi ombor ko'rsatilishi shart",
            "Для перемещения типа {0} нужно указать склад-получатель"],
        ["{0} transfer must have a source warehouse"] = [
            "{0} transferida jo'natuvchi ombor ko'rsatilishi shart",
            "Для перемещения типа {0} нужно указать склад-отправитель"],
        ["Amount exceeds the outstanding commission ({0:N0})"] = [
            "Summa to'lanmagan komissiyadan ({0:N0}) ko'p",
            "Сумма превышает невыплаченную комиссию ({0:N0})"],
        ["Amount must cover whole commission records; {0:N0} is left uncovered. Next payable record: {1}"] = [
            "Summa komissiya yozuvlarini to'liq qoplashi kerak; {0:N0} qoplanmay qoldi. Keyingi yozuv: {1}",
            "Сумма должна покрывать записи комиссии целиком; не покрыто {0:N0}. Следующая запись: {1}"],

        // ── Subscription and entitlements (what a blocked customer reads) ──
        [Messages.TenantMissing] = [
            "Bu hisob endi mavjud emas. Iltimos, biz bilan bog'laning.",
            "Эта учётная запись больше не существует. Пожалуйста, свяжитесь с нами."],
        [Messages.TenantInactive] = [
            "Bu hisob faolsizlantirilgan. Iltimos, biz bilan bog'laning.",
            "Эта учётная запись отключена. Пожалуйста, свяжитесь с нами."],
        [Messages.TrialExpired] = [
            "Sinov muddati tugadi. Davom etish uchun tarif tanlang.",
            "Пробный период закончился. Выберите тариф, чтобы продолжить."],
        [Messages.PaymentExpired] = [
            "To'lov muddati tugadi. Yangilash uchun biz bilan bog'laning.",
            "Оплаченный период закончился. Свяжитесь с нами для продления."],
        [Messages.SuspendedNonPayment] = [
            "Obuna to'lov kechikkani uchun to'xtatilgan. Kirishni tiklash uchun biz bilan bog'laning.",
            "Подписка приостановлена из-за неоплаты. Свяжитесь с нами, чтобы восстановить доступ."],
        [Messages.SuspendedClientRequest] = [
            "Hisobingiz o'z so'rovingizga binoan to'xtatib turilgan. Qayta yoqish uchun bog'laning.",
            "Ваша учётная запись приостановлена по вашей просьбе. Напишите нам, когда захотите её включить."],
        [Messages.SuspendedTechnical] = [
            "Tizimda texnik ishlar olib borilmoqda. Birozdan so'ng qayta urinib ko'ring.",
            "В системе ведутся технические работы. Попробуйте позже."],
        [Messages.SuspendedViolation] = [
            "Hisobingiz to'xtatilgan. Iltimos, biz bilan bog'laning.",
            "Ваша учётная запись приостановлена. Пожалуйста, свяжитесь с нами."],
        [Messages.SuspendedOther] = [
            "Obuna to'xtatilgan. Kirishni tiklash uchun biz bilan bog'laning.",
            "Подписка приостановлена. Свяжитесь с нами, чтобы восстановить доступ."],
        [Messages.ModuleDisabled] = [
            "Bu modul sizning tarifingizga kirmaydi ({0})",
            "Этот модуль не входит в ваш тариф ({0})"],
        [Messages.FeatureDisabled] = [
            "Bu imkoniyat sizning tarifingizga kirmaydi ({0})",
            "Эта возможность не входит в ваш тариф ({0})"],
        [Messages.LimitUsers] = [
            "Tarifingiz ({0}) {1} ta foydalanuvchiga ruxsat beradi. Ko'proq qo'shish uchun tarifni oshiring.",
            "Ваш тариф ({0}) допускает {1} пользователей. Повысьте тариф, чтобы добавить ещё."],
        [Messages.LimitWarehouses] = [
            "Tarifingiz ({0}) {1} ta omborga ruxsat beradi. Ko'proq qo'shish uchun tarifni oshiring.",
            "Ваш тариф ({0}) допускает {1} складов. Повысьте тариф, чтобы добавить ещё."],
        [Messages.LimitTransfers] = [
            "Tarifingiz ({0}) oyiga {1} ta harakatga ruxsat beradi. Davom etish uchun tarifni oshiring.",
            "Ваш тариф ({0}) допускает {1} перемещений в месяц. Повысьте тариф, чтобы продолжить."],
        [Messages.LimitUsersImport] = [
            "Tarif limiti to'ldi — ko'proq foydalanuvchi qo'shish uchun tarifni oshiring",
            "Достигнут лимит тарифа — повысьте тариф, чтобы добавить больше пользователей"],

        // ── Branding ──
        [Messages.InvalidBrandColor] = [
            "Brend rangi #2E7D32 kabi hex qiymat bo'lishi kerak",
            "Цвет бренда должен быть hex-значением, например #2E7D32"],
        [Messages.LogoEmpty] = ["Yuklangan fayl bo'sh", "Загруженный файл пуст"],
        [Messages.LogoTooLarge] = [
            "Logo hajmi {0} KB dan oshmasligi kerak",
            "Размер логотипа не должен превышать {0} КБ"],
        [Messages.LogoFormat] = [
            "Logo SVG, PNG yoki WebP rasm bo'lishi kerak",
            "Логотип должен быть изображением SVG, PNG или WebP"],
        [Messages.LogoUnsafeSvg] = [
            "Bu SVG ichida skript yoki tashqi havola bor — logo sifatida ishlatib bo'lmaydi",
            "В этом SVG есть скрипты или внешние ссылки — его нельзя использовать как логотип"],
        [Messages.LogoTooBig] = [
            "Logo o'lchami ko'pi bilan {0}×{1} px bo'lsin (bu {2}×{3})",
            "Размер логотипа не более {0}×{1} px (у этого {2}×{3})"],

        // ── Limit warnings ──
        [Messages.LimitWarnUsers] = [
            "Tarif limitiga yaqinlashdingiz: {1} tadan {0} ta foydalanuvchi",
            "Вы приближаетесь к лимиту тарифа: {0} из {1} пользователей"],
        [Messages.LimitWarnWarehouses] = [
            "Tarif limitiga yaqinlashdingiz: {1} tadan {0} ta ombor",
            "Вы приближаетесь к лимиту тарифа: {0} из {1} складов"],
        [Messages.PermissionsOutsidePlan] = [
            "Tanlangan ruxsatlardan {0} tasi tarifingizga kirmaydi va tarif kengaytirilmaguncha ishlamaydi",
            "{0} из выбранных разрешений не входят в ваш тариф и не будут работать до его расширения"],
        [Messages.LimitWarnTransfers] = [
            "Tarif limitiga yaqinlashdingiz: bu oyda {1} tadan {0} ta harakat",
            "Вы приближаетесь к лимиту тарифа: {0} из {1} перемещений в этом месяце"],

        // ── Password reset ──
        [Messages.PasswordTooShort] = [
            "Parol kamida {0} ta belgidan iborat bo'lishi kerak",
            "Пароль должен содержать не менее {0} символов"],
        [Messages.CannotResetPlatformUser] = [
            "Bu platforma hisobi — uni faqat platforma administratori tiklay oladi",
            "Это платформенная учётная запись — сбросить её может только администратор платформы"],
        [Messages.UseChangePasswordInstead] = [
            "O'z hisobingiz uchun \"parolni o'zgartirish\" dan foydalaning",
            "Для своей учётной записи используйте «смену пароля»"],
        ["Password reset"] = ["Parol tiklandi", "Пароль сброшен"],

        // ── Custom features ──
        [Messages.CustomFeaturePrefix] = [
            "Maxsus imkoniyat kodi '{0}' bilan boshlanishi kerak",
            "Код особой возможности должен начинаться с '{0}'"],
        [Messages.CustomFeatureInPlan] = [
            "'{0}' — maxsus imkoniyat, u tarifga qo'shilmaydi; uni tenantga alohida bering",
            "'{0}' — особая возможность, её нельзя включить в тариф; выдайте её конкретному клиенту"],
        [Messages.UnknownFeatureCode] = [
            "Noma'lum imkoniyat kodi '{0}'",
            "Неизвестный код возможности '{0}'"],
        [Messages.FeatureNotFound] = [
            "'{0}' imkoniyati topilmadi",
            "Возможность '{0}' не найдена"],
        ["A custom feature must have DefaultEnabled = false — it is granted per tenant, never by default"] = [
            "Maxsus imkoniyatda DefaultEnabled = false bo'lishi shart — u har tenantga alohida beriladi",
            "У особой возможности DefaultEnabled должен быть false — она выдаётся каждому клиенту отдельно"],
        ["A custom feature must name the tenant it was written for (OwnerTenantId)"] = [
            "Maxsus imkoniyat kim uchun yozilganini ko'rsatishi shart (OwnerTenantId)",
            "Особая возможность должна указывать, для кого написана (OwnerTenantId)"],
        ["A feature with this code already exists"] = [
            "Bu kodli imkoniyat allaqachon mavjud",
            "Возможность с таким кодом уже существует"],
        ["Feature code is required"] = ["Imkoniyat kodi majburiy", "Код возможности обязателен"],
        ["Feature name is required"] = ["Imkoniyat nomi majburiy", "Название возможности обязательно"],

        // ── Auth ──
        ["Invalid credentials"] = ["Login yoki parol noto'g'ri", "Неверный логин или пароль"],
        ["Invalid credentials or portal not enabled"] = [
            "Login/parol noto'g'ri yoki portal yoqilmagan",
            "Неверный логин/пароль или портал не включён"],
        ["Current password is incorrect"] = ["Joriy parol noto'g'ri", "Текущий пароль неверен"],
        ["New password must be at least 6 characters"] = [
            "Yangi parol kamida 6 belgidan iborat bo'lsin",
            "Новый пароль должен содержать не менее 6 символов"],
        ["Admin password must be at least 6 characters"] = [
            "Admin paroli kamida 6 belgidan iborat bo'lsin",
            "Пароль администратора должен содержать не менее 6 символов"],
        ["Phone is required"] = ["Telefon raqami majburiy", "Телефон обязателен"],
        ["Phone number already belongs to another user"] = [
            "Bu telefon raqami boshqa foydalanuvchiga tegishli",
            "Этот номер телефона принадлежит другому пользователю"],
        ["A user with this phone already exists"] = [
            "Bu telefon raqamli foydalanuvchi allaqachon mavjud",
            "Пользователь с таким телефоном уже существует"],
        ["Not found"] = ["Topilmadi", "Не найдено"],

        // ── Tenant / plan / platform ──
        ["Tenant not found"] = ["Tashkilot topilmadi", "Организация не найдена"],
        ["Tenant name is required"] = ["Tashkilot nomi majburiy", "Название организации обязательно"],
        ["Slug is required"] = ["Slug majburiy", "Slug обязателен"],
        ["Slug may only contain lowercase letters, digits and hyphens (3-50 chars)"] = [
            "Slug faqat kichik lotin harflari, raqam va chiziqchadan iborat bo'lsin (3-50 belgi)",
            "Slug может содержать только строчные латинские буквы, цифры и дефис (3-50 символов)"],
        ["This slug is already taken"] = ["Bu slug band", "Этот slug уже занят"],
        ["This slug is reserved, please choose another"] = [
            "Bu slug zaxiralangan, boshqasini tanlang",
            "Этот slug зарезервирован, выберите другой"],
        ["Plan not found"] = ["Tarif topilmadi", "Тариф не найден"],
        ["Plan name is required"] = ["Tarif nomi majburiy", "Название тарифа обязательно"],
        ["Plan code is required"] = ["Tarif kodi majburiy", "Код тарифа обязателен"],
        ["A plan with this code already exists"] = [
            "Bu kodli tarif allaqachon mavjud", "Тариф с таким кодом уже существует"],
        ["This plan is still assigned to one or more tenants"] = [
            "Bu tarif hali bir yoki bir nechta tashkilotga biriktirilgan",
            "Этот тариф ещё назначен одной или нескольким организациям"],
        ["This is the default registration plan — make another plan default first"] = [
            "Bu — ro'yxatdan o'tish uchun default tarif; avval boshqasini default qiling",
            "Это тариф по умолчанию для регистрации — сначала назначьте другой"],
        ["Organization not found"] = ["Kompaniya topilmadi", "Компания не найдена"],
        ["INN (STIR) must be exactly 9 digits"] = [
            "STIR aynan 9 ta raqamdan iborat bo'lishi kerak",
            "ИНН должен состоять ровно из 9 цифр"],
        ["Payment record not found"] = ["To'lov yozuvi topilmadi", "Запись об оплате не найдена"],
        ["Period end must be after period start"] = [
            "Davr oxiri boshidan keyin bo'lishi kerak",
            "Конец периода должен быть позже начала"],
        ["Amount must be greater than zero"] = ["Summa noldan katta bo'lishi kerak", "Сумма должна быть больше нуля"],
        ["Lead not found"] = ["So'rov topilmadi", "Заявка не найдена"],
        ["This lead has already been converted"] = [
            "Bu so'rov allaqachon tashkilotga aylantirilgan",
            "Эта заявка уже превращена в организацию"],
        ["Company name is required"] = ["Kompaniya nomi majburiy", "Название компании обязательно"],
        ["Contact name is required"] = ["Aloqa uchun ism majburiy", "Имя контактного лица обязательно"],
        ["Phone is required so we can call you back"] = [
            "Sizga qo'ng'iroq qilishimiz uchun telefon raqami kerak",
            "Укажите телефон, чтобы мы могли перезвонить"],

        // ── Users / roles ──
        ["User not found"] = ["Foydalanuvchi topilmadi", "Пользователь не найден"],
        ["Role not found"] = ["Rol topilmadi", "Роль не найдена"],
        ["Worker not found"] = ["Ishchi topilmadi", "Сотрудник не найден"],
        ["Assigned user not found"] = ["Biriktirilgan foydalanuvchi topilmadi", "Назначенный пользователь не найден"],

        // ── Products / catalogue ──
        ["Product not found"] = ["Mahsulot topilmadi", "Товар не найден"],
        ["One or more products not found"] = [
            "Bir yoki bir nechta mahsulot topilmadi", "Один или несколько товаров не найдены"],
        ["Category not found"] = ["Kategoriya topilmadi", "Категория не найдена"],
        ["Parent category not found"] = ["Yuqori kategoriya topilmadi", "Родительская категория не найдена"],
        ["A category cannot be its own parent"] = [
            "Kategoriya o'zining yuqori kategoriyasi bo'la olmaydi",
            "Категория не может быть родителем самой себе"],
        ["Unit not found"] = ["O'lchov birligi topilmadi", "Единица измерения не найдена"],
        ["One or more units not found"] = [
            "Bir yoki bir nechta o'lchov birligi topilmadi", "Одна или несколько единиц измерения не найдены"],

        // ── Counterparties / agents ──
        ["Counterparty not found"] = ["Kontragent topilmadi", "Контрагент не найден"],
        ["Agent not found"] = ["Agent topilmadi", "Агент не найден"],
        ["Commission record not found"] = ["Komissiya yozuvi topilmadi", "Запись комиссии не найдена"],
        ["Commission percent must be between 0 and 100"] = [
            "Komissiya foizi 0 va 100 orasida bo'lishi kerak",
            "Процент комиссии должен быть от 0 до 100"],

        // ── Warehouse ──
        ["Warehouse not found"] = ["Ombor topilmadi", "Склад не найден"],
        ["One or more warehouses not found"] = [
            "Bir yoki bir nechta ombor topilmadi", "Один или несколько складов не найдены"],
        ["Cannot delete warehouse: it still has stock"] = [
            "Omborni o'chirib bo'lmaydi: unda hali qoldiq bor",
            "Нельзя удалить склад: на нём ещё есть остатки"],
        ["Location not found"] = ["Joylashuv topilmadi", "Ячейка не найдена"],
        ["Cannot delete location: it still has stock"] = [
            "Joylashuvni o'chirib bo'lmaydi: unda hali qoldiq bor",
            "Нельзя удалить ячейку: в ней ещё есть остатки"],
        ["Batch not found"] = ["Partiya topilmadi", "Партия не найдена"],
        ["Cannot delete batch: it has remaining stock in warehouse"] = [
            "Partiyani o'chirib bo'lmaydi: omborda qoldig'i bor",
            "Нельзя удалить партию: по ней есть остаток на складе"],
        ["LotNumber is required"] = ["Partiya raqami majburiy", "Номер партии обязателен"],
        ["LotNumber already exists for this product"] = [
            "Bu mahsulot uchun bunday partiya raqami allaqachon mavjud",
            "Такой номер партии уже существует для этого товара"],
        ["No finished goods warehouse configured for this tenant"] = [
            "Bu tashkilot uchun tayyor mahsulot ombori sozlanmagan",
            "Для этой организации не настроен склад готовой продукции"],

        // ── Transfers ──
        ["Transfer not found"] = ["Harakat topilmadi", "Перемещение не найдено"],
        ["Transfer must contain at least one item"] = [
            "Harakatda kamida bitta qator bo'lishi kerak",
            "В перемещении должна быть хотя бы одна позиция"],
        ["Item quantity must be greater than zero"] = [
            "Qator miqdori noldan katta bo'lishi kerak", "Количество в позиции должно быть больше нуля"],
        ["Item price cannot be negative"] = ["Qator narxi manfiy bo'lmasligi kerak", "Цена позиции не может быть отрицательной"],
        ["Quantities cannot be negative"] = ["Miqdorlar manfiy bo'lmasligi kerak", "Количества не могут быть отрицательными"],
        ["Only pending transfers can be confirmed"] = [
            "Faqat kutilayotgan harakatni tasdiqlash mumkin",
            "Подтвердить можно только перемещение в статусе «ожидание»"],
        ["Only pending transfers can be rejected"] = [
            "Faqat kutilayotgan harakatni rad etish mumkin",
            "Отклонить можно только перемещение в статусе «ожидание»"],
        ["Only pending transfers can be cancelled"] = [
            "Faqat kutilayotgan harakatni bekor qilish mumkin",
            "Отменить можно только перемещение в статусе «ожидание»"],
        ["Incoming transfer must have a destination warehouse"] = [
            "Kirim harakatida qabul qiluvchi ombor ko'rsatilishi shart",
            "У прихода должен быть склад назначения"],
        ["Outgoing transfer must have a source warehouse"] = [
            "Chiqim harakatida jo'natuvchi ombor ko'rsatilishi shart",
            "У расхода должен быть склад отправления"],
        ["Internal transfer must have a destination warehouse"] = [
            "Ichki harakatda qabul qiluvchi ombor ko'rsatilishi shart",
            "У внутреннего перемещения должен быть склад назначения"],
        ["Internal transfer must have both source and destination warehouses"] = [
            "Ichki harakatda ikkala ombor ham ko'rsatilishi shart",
            "У внутреннего перемещения должны быть оба склада"],
        ["Internal transfer source and destination must differ"] = [
            "Ichki harakatda omborlar bir xil bo'lmasligi kerak",
            "Склады внутреннего перемещения должны различаться"],
        ["Return transfer must have a destination warehouse"] = [
            "Qaytarishda qabul qiluvchi ombor ko'rsatilishi shart",
            "У возврата должен быть склад назначения"],
        ["Return counterparty must match the original sale"] = [
            "Qaytarish kontragenti asl sotuv bilan bir xil bo'lishi kerak",
            "Контрагент возврата должен совпадать с исходной продажей"],
        ["Original transfer not found"] = ["Asl harakat topilmadi", "Исходное перемещение не найдено"],
        ["Original transfer must be a confirmed outgoing sale"] = [
            "Asl harakat tasdiqlangan chiqim bo'lishi kerak",
            "Исходное перемещение должно быть подтверждённой продажей"],
        ["OriginalTransferId is only valid for return transfers"] = [
            "OriginalTransferId faqat qaytarish uchun ishlatiladi",
            "OriginalTransferId применим только к возвратам"],

        // ── Production ──
        ["Recipe not found"] = ["Retsept topilmadi", "Рецепт не найден"],
        ["Stage not found"] = ["Bosqich topilmadi", "Этап не найден"],
        ["One or more production stages not found"] = [
            "Bir yoki bir nechta bosqich topilmadi", "Один или несколько этапов не найдены"],
        ["Stage execution not found"] = ["Bosqich bajarilishi topilmadi", "Выполнение этапа не найдено"],
        ["Order not found"] = ["Buyurtma topilmadi", "Заказ не найден"],
        ["Production order not found"] = ["Ishlab chiqarish buyurtmasi topilmadi", "Производственный заказ не найден"],
        ["Only draft orders can be started"] = [
            "Faqat qoralama buyurtmani boshlash mumkin", "Запустить можно только черновик заказа"],
        ["Order must be in progress"] = ["Buyurtma jarayonda bo'lishi kerak", "Заказ должен быть в работе"],
        ["All stages must be completed or skipped before completing the order"] = [
            "Buyurtmani yakunlashdan oldin barcha bosqichlar tugatilgan yoki o'tkazib yuborilgan bo'lishi kerak",
            "Перед завершением заказа все этапы должны быть выполнены или пропущены"],
        ["Planned quantity must be greater than zero"] = [
            "Rejalashtirilgan miqdor noldan katta bo'lishi kerak", "Плановое количество должно быть больше нуля"],

        // ── Quality ──
        ["QC parameter not found"] = ["Sifat parametri topilmadi", "Параметр контроля не найден"],
        ["QC check must have either StageExecutionId or TransferId"] = [
            "Sifat tekshiruvida StageExecutionId yoki TransferId bo'lishi shart",
            "У проверки качества должен быть либо StageExecutionId, либо TransferId"],
        ["QC check must have either StageExecutionId or TransferId, not both"] = [
            "Sifat tekshiruvida faqat bittasi bo'lsin: StageExecutionId yoki TransferId",
            "У проверки качества должно быть только одно: StageExecutionId или TransferId"],

        // ── Finance / KPI ──
        ["Transaction not found"] = ["Amaliyot topilmadi", "Операция не найдена"],
        ["Shift not found"] = ["Smena topilmadi", "Смена не найдена"],
        ["Attendance log not found"] = ["Davomat yozuvi topilmadi", "Запись посещаемости не найдена"],

        // ── Delivery ──
        ["Delivery not found"] = ["Yetkazib berish topilmadi", "Доставка не найдена"],
        ["Delivery stop not found"] = ["Yetkazib berish nuqtasi topilmadi", "Точка доставки не найдена"],
        ["Delivery must contain at least one stop"] = [
            "Yetkazib berishda kamida bitta nuqta bo'lishi kerak", "В доставке должна быть хотя бы одна точка"],
        ["Only planned deliveries can be deleted"] = [
            "Faqat rejalashtirilgan yetkazib berishni o'chirish mumkin",
            "Удалить можно только запланированную доставку"],
        ["Vehicle not found"] = ["Transport topilmadi", "Транспорт не найден"],
        ["Vehicle name is required"] = ["Transport nomi majburiy", "Название транспорта обязательно"],
        ["Driver not found"] = ["Haydovchi topilmadi", "Водитель не найден"],
        ["Driver name is required"] = ["Haydovchi ismi majburiy", "Имя водителя обязательно"],

        // ── Success messages returned in ApiResponse.Message ──
        ["Deleted"] = ["O'chirildi", "Удалено"],
        ["Updated"] = ["Yangilandi", "Обновлено"],
        ["Cancelled"] = ["Bekor qilindi", "Отменено"],
        ["Created"] = ["Yaratildi", "Создано"],
        ["Saved"] = ["Saqlandi", "Сохранено"],
        ["Profile updated"] = ["Profil yangilandi", "Профиль обновлён"],
        ["Password changed"] = ["Parol o'zgartirildi", "Пароль изменён"],
        ["Roles assigned"] = ["Rollar biriktirildi", "Роли назначены"],
        ["Permissions assigned"] = ["Ruxsatlar biriktirildi", "Права назначены"],
        ["Status updated"] = ["Holat yangilandi", "Статус обновлён"],
        ["Reordered"] = ["Tartib o'zgartirildi", "Порядок изменён"],
        ["Marked as read"] = ["O'qilgan deb belgilandi", "Отмечено как прочитанное"],
        ["Commission paid"] = ["Komissiya to'landi", "Комиссия выплачена"],
        ["Payment recorded"] = ["To'lov qayd etildi", "Оплата записана"],
        ["Payment cancelled"] = ["To'lov bekor qilindi", "Оплата отменена"],
        ["Tenant created"] = ["Tashkilot yaratildi", "Организация создана"],
        ["Registration successful"] = ["Ro'yxatdan o'tish muvaffaqiyatli", "Регистрация выполнена"],
        ["Thank you — we will contact you shortly"] = [
            "Rahmat — tez orada siz bilan bog'lanamiz", "Спасибо — мы скоро свяжемся с вами"],

        // ── Generic API responses ──
        ["Unauthorized"] = ["Ruxsat yo'q", "Не авторизовано"],
        ["Permission denied"] = ["Ruxsat berilmagan", "Доступ запрещён"],
        ["An unexpected error occurred"] = ["Kutilmagan xatolik yuz berdi", "Произошла непредвиденная ошибка"],
        ["You can only view your own tenant's modules"] = [
            "Faqat o'z tashkilotingiz modullarini ko'rishingiz mumkin",
            "Вы можете просматривать модули только своей организации"]
    };

    private static int Index(string lang) => lang switch { Lang.Uz => 0, Lang.Ru => 1, _ => -1 };

    /// <summary>
    /// Translates a message and fills in its arguments. English (or an unknown language)
    /// returns the template as written; an untranslated message also returns English —
    /// a customer reading English is a small problem, a missing message is a big one.
    /// </summary>
    public static string Format(string? template, string? lang, params object?[] args)
    {
        if (string.IsNullOrEmpty(template)) return "";

        var text = template;
        var index = Index(Lang.Normalize(lang) ?? Lang.Default);
        if (index >= 0 && Map.TryGetValue(template, out var translations) && translations.Length > index
            && !string.IsNullOrEmpty(translations[index]))
            text = translations[index];

        if (args is not { Length: > 0 }) return text;

        try
        {
            return string.Format(text, args);
        }
        catch (FormatException)
        {
            // A mistyped placeholder must never turn an error message into a 500.
            return text;
        }
    }

    /// True when this exact text is known — used by tests and tooling to spot messages
    /// that were added to the code but never translated.
    public static bool Has(string text) => Map.ContainsKey(text);

    public static int Count => Map.Count;
}
