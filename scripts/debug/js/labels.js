/** Подписи формы «Новая заявка» — зеркало Resources.resw (ru-RU). */
export const labels = {
  pageTitle: "CMMS — отладочная панель",
  loginTitle: "Вход",
  email: "E-mail",
  password: "Пароль",
  signIn: "Войти",
  signOut: "Выйти",
  session: "Сессия",
  newRequestSection: "Новая заявка",
  type: "Тип заявки",
  priority: "Приоритет",
  repairDepartment: "Отдел ремонта",
  repairDepartmentPlaceholder: "Выберите отдел ремонта",
  subject: "Объект заявки",
  subjectAsset: "Оборудование",
  subjectLocation: "Инфраструктура",
  assetPlaceholder: "Номер или название оборудования",
  locationPlaceholder: "Опишите место или объект без инвентарного номера",
  title: "Краткое описание",
  description: "Подробное описание",
  repairZone: "Зона ремонта",
  contractorName: "Подрядчик",
  contractorPlaceholder: "Наименование подрядчика",
  submit: "Создать заявку",
  randomTitle: "Случайное название",
  randomAsset: "Случайное оборудование",
  log: "Журнал",
};

export const requestTypes = [
  { value: "breakdown", label: "Поломка" },
  { value: "service", label: "Обслуживание" },
  { value: "inspection", label: "Осмотр" },
];

export const requestPriorities = [
  { value: "low", label: "Низкий" },
  { value: "normal", label: "Обычный" },
  { value: "high", label: "Высокий" },
  { value: "critical", label: "Критический" },
];

export const repairZones = [
  { value: "on_site", label: "На месте" },
  { value: "workshop", label: "В ремонтном цехе" },
  { value: "external", label: "У внешнего подрядчика" },
];
