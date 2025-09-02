// =================================================================================
// File: Services/AutoCADService.cs
// Description: Сервисный класс, инкапсулирующий всю логику взаимодействия с AutoCAD API.
//              ВЕРСИЯ ИСПРАВЛЕНА для потокобезопасного выполнения вызовов API.
// =================================================================================
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AutoCADLayerManager.Models;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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
        // Потокобезопасная очередь для действий, которые должны выполняться в главном потоке AutoCAD.
        private static readonly ConcurrentQueue<Action> _actionsToExecuteOnMainThread = new ConcurrentQueue<Action>();

        public event EventHandler<LayerChangedEventArgs> LayerChanged;

        public AutoCADService()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Database.ObjectAppended += OnObjectAppended;
                doc.Database.ObjectModified += OnObjectModified;
                doc.Database.ObjectErased += OnObjectErased;
            }
            // Подписываемся на событие Idle, которое гарантированно выполняется в главном потоке AutoCAD.
            AcApp.Idle += OnAutoCADIdle;
        }

        public void Cleanup()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Database.ObjectAppended -= OnObjectAppended;
                doc.Database.ObjectModified -= OnObjectModified;
                doc.Database.ObjectErased -= OnObjectErased;
            }
            // Обязательно отписываемся от события, чтобы избежать утечек памяти.
            AcApp.Idle -= OnAutoCADIdle;
        }

        // Этот обработчик выполняется в главном потоке и разбирает очередь заданий.
        private void OnAutoCADIdle(object sender, EventArgs e)
        {
            while (_actionsToExecuteOnMainThread.TryDequeue(out Action action))
            {
                try
                {
                    action.Invoke();
                }
                catch (System.Exception ex)
                {
                    Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage($"\nОшибка при выполнении действия в основном потоке: {ex.Message}");
                }
            }
        }

        #region Event Handlers for Two-Way Sync (без изменений)
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
                var layerData = new LayerData { Name = ltr.Name, IsOff = ltr.IsOff, Color = ltr.Color };
                LayerChanged?.Invoke(this, new LayerChangedEventArgs(ltr.Name, LayerChangeType.Modified, layerData));
            }
        }
        private void OnObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is LayerTableRecord ltr)
            {
                var layerData = new LayerData { Name = ltr.Name, IsOff = ltr.IsOff, Color = ltr.Color };
                LayerChanged?.Invoke(this, new LayerChangedEventArgs(ltr.Name, LayerChangeType.Added, layerData));
            }
        }
        #endregion

        // Метод GetLayers остается синхронным, так как чтение данных внутри транзакции обычно потокобезопасно.
        public List<LayerData> GetLayers()
        {
            var layers = new List<LayerData>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return layers;
            var db = doc.Database;
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (layerTable == null) return layers;
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    if (layer == null) continue;
                    layers.Add(new LayerData { Name = layer.Name, Color = layer.Color, IsOff = layer.IsOff });
                }
                transaction.Commit();
            }
            return layers;
        }

        // Все методы, которые изменяют чертеж или взаимодействуют с редактором, теперь добавляют свою логику в очередь.
        public void UpdateLayerProperty(string layerName, Action<LayerTableRecord> updateAction)
        {
            _actionsToExecuteOnMainThread.Enqueue(() =>
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                using (doc.LockDocument())
                {
                    using (var tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        var lt = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                        if (lt != null && lt.Has(layerName))
                        {
                            var lr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                            updateAction(lr);
                        }
                        tr.Commit();
                    }
                }
                doc.Editor.Regen();
            });
        }

        public void CreateLayer(string layerName, Autodesk.AutoCAD.Colors.Color color)
        {
            _actionsToExecuteOnMainThread.Enqueue(() =>
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                using (doc.LockDocument())
                {
                    // ... (здесь вся логика из предыдущей версии метода)
                }
            });
        }

        public void DeleteLayer(string layerName)
        {
            _actionsToExecuteOnMainThread.Enqueue(() =>
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                using (doc.LockDocument())
                {
                    // ... (здесь вся логика из предыдущей версии метода)
                }
            });
        }

        public void HighlightObjectsOnLayer(string layerName)
        {
            _actionsToExecuteOnMainThread.Enqueue(() =>
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                var ed = doc.Editor;
                var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.LayerName, layerName) });
                var selectionResult = ed.SelectAll(filter);
                if (selectionResult.Status == PromptStatus.OK)
                {
                    ed.SetImpliedSelection(selectionResult.Value.GetObjectIds());
                    ed.WriteMessage($"\nПодсвечено {selectionResult.Value.Count} объектов на слое '{layerName}'.");
                }
                else
                {
                    ed.SetImpliedSelection(new ObjectId[0]);
                    ed.WriteMessage($"\nНа слое '{layerName}' не найдено объектов для подсветки.");
                }
            });
        }
    }
}
