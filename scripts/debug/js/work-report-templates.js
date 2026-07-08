import { pickRandom } from "./random.js";

const workPerformedLines = [
  "Выполнена диагностика узлов, устранены люфты креплений",
  "Заменены изношенные уплотнения, проверена герметичность",
  "Отрегулированы зазоры, выполнена пробная наладка",
  "Проведена чистка и смазка направляющих, проверка хода",
  "Заменён приводной ремень, проверена натяжка",
  "Устранено замыкание в цепи управления, восстановлен пуск",
  "Выполнена балансировка вращающихся частей",
  "Проведена замена фильтрующих элементов гидросистемы",
  "Наладка ЧПУ, калибровка нулевых точек осей",
  "Замена подшипников шпинделя, контроль биения",
];

const defectsFoundLines = [
  "Износ уплотнений гидроцилиндра",
  "Ослабление крепёжных болтов суппорта",
  "Повышенный люфт в подшипниках",
  "Загрязнение масла в редукторе",
  "Перегрев двигателя при длительной работе",
  null,
  null,
];

const notesLines = [
  "Рекомендован контроль через 40 моточасов",
  "Требуется заказ ЗИП на следующий цикл",
  "Станок допущен к эксплуатации",
  "Повторная проверка после смены",
  null,
];

const partsLines = [
  "Сальник 45×62×10 — 2 шт.",
  "Ремень клиновой B-1600 — 1 шт.",
  "Подшипник 6205-2RS — 2 шт.",
  "Фильтр гидравлический HF-204 — 1 шт.",
  "Масло И-40А — 3 л",
];

/** @returns {{ work_performed: string, actual_duration_hours: number, defects_found: string | null, notes: string | null, parts_used: string | null, maintenance_type?: string | null }} */
export function buildRandomWorkReport(maintenanceType = null) {
  const durationChoices = [0.5, 1, 1.5, 2, 2.5, 3, 4, 5, 6, 8];
  return {
    work_performed: pickRandom(workPerformedLines) ?? "Выполнены регламентные работы",
    actual_duration_hours: pickRandom(durationChoices) ?? 2,
    defects_found: pickRandom(defectsFoundLines) ?? null,
    notes: pickRandom(notesLines) ?? null,
    parts_used: Math.random() > 0.35 ? pickRandom(partsLines) ?? null : null,
    maintenance_type: maintenanceType,
  };
}
