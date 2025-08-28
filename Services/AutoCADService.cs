// =================================================================================
// File: Services/AutoCADService.cs
// Description: Сервисный класс, инкапсулирующий всю логику взаимодействия с AutoCAD API.
// =================================================================================
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using AutoCADLayerManager.Models;

namespace AutoCADLayerManager.Services
{
    // Класс для передачи данных о событии изменения слоя
    public class LayerChangedEventArgs : EventArgs
    {
        public string LayerName { get; }
        public LayerChangeType ChangeType { get; }
        public LayerData NewData { get; set; }

        public LayerChangedEventArgs(string layerName, LayerChangeType changeType, LayerData newData = null)
        {
            LayerName = layerName;
            ChangeType = changeType;
            NewData = newData;
        }
    }

    public enum LayerChangeType { Added, Modified, Erased }

    public class AutoCADService
    {
        // Событие, уведомляющее подписчиков (ViewModel) об изменениях в слоях чертежа
        public event EventHandler<LayerChangedEventArgs> LayerChanged;

        public AutoCADService()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                // Подписываемся на события базы данных для отслеживания изменений
                doc.Database.ObjectAppended += OnObjectAppended;
                doc.Database.ObjectModified += OnObjectModified;
                doc.Database.ObjectErased += OnObjectErased;
            }
        }

        // Метод для очистки подписок на события
        public void Cleanup()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Database.ObjectAppended -= OnObjectAppended;
                doc.Database.ObjectModified -= OnObjectModified;
                doc.Database.ObjectErased -= OnObjectErased;
            }
        }

        #region Event Handlers for Two-Way Sync

        private void OnObjectErased(object sender, ObjectErasedEventArgs e)
        {
            if (e.DBObject is LayerTableRecord ltr)
            {
                LayerChanged?.Invoke(this, new LayerChangedEventArgs(ltr.Name, LayerChangeType.Erased));
            }
        }

        private void OnObjectModified(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is LayerTableRecord ltr)
            {
                var layerData = new LayerData
                {
                    Name = ltr.Name,
                    IsOff = ltr.IsOff,
                    Color = ltr.Color
                };
                LayerChanged?.Invoke(this, new LayerChangedEventArgs(ltr.Name, LayerChangeType.Modified, layerData));
            }
        }

        private void OnObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is LayerTableRecord ltr)
            {
                var layerData = new LayerData
                {
                    Name = ltr.Name,
                    IsOff = ltr.IsOff,
                    Color = ltr.Color
                };
                LayerChanged?.Invoke(this, new LayerChangedEventArgs(ltr.Name, LayerChangeType.Added, layerData));
            }
        }

        #endregion

        // Получение списка всех слоев из текущего чертежа
        public List<LayerData> GetLayers()
        {
            var layers = new List<LayerData>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return layers;
            var db = doc.Database;

            // Все операции с базой данных AutoCAD должны выполняться внутри транзакции
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    layers.Add(new LayerData
                    {
                        Name = layer.Name,
                        Color = layer.Color,
                        IsOff = layer.IsOff
                    });
                }
                transaction.Commit(); // Завершаем транзакцию
            }
            return layers;
        }

        // Универсальный метод для обновления свойства слоя
        public void UpdateLayerProperty(string layerName, Action<LayerTableRecord> updateAction)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;
            var ed = doc.Editor;

            // Блокируем документ на время выполнения операции для потокобезопасности
            using (doc.LockDocument())
            {
                using (var transaction = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        var layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                        if (layerTable.Has(layerName))
                        {
                            var layerId = layerTable[layerName];
                            // Открываем объект слоя для записи
                            var layer = transaction.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                            updateAction(layer); // Выполняем переданное действие (например, меняем цвет)
                        }
                        transaction.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nОшибка обновления слоя: {ex.Message}");
                        transaction.Abort(); // Откатываем транзакцию в случае ошибки
                    }
                }
            }
            ed.Regen(); // Обновляем экран для отображения изменений
        }

        // Создание нового слоя
        public void CreateLayer(string layerName, Autodesk.AutoCAD.Colors.Color color)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;
            var ed = doc.Editor;

            using (doc.LockDocument())
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    if (!lt.Has(layerName))
                    {
                        var ltr = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = color
                        };
                        lt.Add(ltr);
                        tr.AddNewlyCreatedDBObject(ltr, true);
                        tr.Commit();
                        ed.WriteMessage($"\nСлой '{layerName}' успешно создан.");
                    }
                    else
                    {
                        ed.WriteMessage($"\nСлой с именем '{layerName}' уже существует.");
                    }
                }
            }
        }

        // Удаление слоя
        public void DeleteLayer(string layerName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;
            var ed = doc.Editor;

            using (doc.LockDocument())
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    // ИСПРАВЛЕНО: Получаем имя текущего слоя корректно
                    var cltr = (LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead);
                    string currentLayerName = cltr.Name;

                    // Проверяем, не пытаемся ли мы удалить запрещенные слои
                    if (layerName.Equals("0", StringComparison.OrdinalIgnoreCase) || layerName.Equals(currentLayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        ed.WriteMessage($"\nНельзя удалить активный слой или слой '0'.");
                        tr.Abort(); // Завершаем транзакцию
                        return;
                    }

                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (lt.Has(layerName))
                    {
                        var layerId = lt[layerName];

                        var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.LayerName, layerName) });
                        var selectionResult = ed.SelectAll(filter);
                        if (selectionResult.Status == PromptStatus.OK && selectionResult.Value.Count > 0)
                        {
                            ed.WriteMessage($"\nНельзя удалить слой '{layerName}', так как он содержит объекты.");
                            tr.Abort();
                            return;
                        }

                        try
                        {
                            var ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                            ltr.Erase();
                            tr.Commit();
                            ed.WriteMessage($"\nСлой '{layerName}' удален.");
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\nНе удалось удалить слой '{layerName}': {ex.Message}");
                            tr.Abort();
                        }
                    }
                    else
                    {
                        // Если слоя нет, просто завершаем транзакцию
                        tr.Commit();
                    }
                }
            }
        }

        // Подсветка объектов на выбранном слое
        public void HighlightObjectsOnLayer(string layerName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            // Создаем фильтр для выбора всех объектов на указанном слое
            var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.LayerName, layerName) });
            var selectionResult = ed.SelectAll(filter);

            if (selectionResult.Status == PromptStatus.OK)
            {
                // Устанавливаем "подразумеваемый выбор" (объекты подсвечиваются, но не выделяются)
                ed.SetImpliedSelection(selectionResult.Value.GetObjectIds());
                ed.WriteMessage($"\nПодсвечено {selectionResult.Value.Count} объектов на слое '{layerName}'.");
            }
            else
            {
                // Если объектов нет, очищаем предыдущую подсветку
                ed.SetImpliedSelection(new ObjectId[0]);
                ed.WriteMessage($"\nНа слое '{layerName}' не найдено объектов для подсветки.");
            }

            doc.Editor.UpdateScreen();
        }
    }
}
