using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Templates {
    /// <summary>
    /// Base class for all templates.
    /// GUID is used to identify template in Resources folder.
    /// </summary>
    [DisallowMultipleComponent]
    [HideMonoScript]
    public class Template : MonoBehaviour, ITemplate {
        public const string DefaultAttachmentGroupName = "technical.base.group";
        
        [HideInInspector]
        public TemplateType templateType;
        [SerializeField, HideInInspector]
        bool _isAbstract;
        
        [TemplateType(typeof(Template)), PropertyOrder(-100)] [SerializeField]
        TemplateReference[] _abstractTypes = Array.Empty<TemplateReference>();

        [SerializeField, HideInInspector] TemplateMetadata metadata;
        
        public string GUID { get; set; }
        
        string INamed.DisplayName => string.Empty;
        public string DebugName => this != null ? name : "Destroyed Template";

        public TemplateMetadata Metadata => metadata;

        public PooledList<IAttachmentSpec> DirectAttachments {
            get {
                PooledList<IAttachmentSpec>.Get(out var results);
                GetComponents(results.value);
                return results;
            }
        } 

        public PooledList<ITemplate> DirectAbstracts {
            get {
                PooledList<ITemplate>.Get(out var directAbstracts);
                directAbstracts.value.EnsureCapacity(_abstractTypes.Length);
                foreach(var reference in _abstractTypes) {
                    directAbstracts.value.Add(reference.Get<ITemplate>(this));
                }

                return directAbstracts;
            }
        }
        public bool IsAbstract => _isAbstract;
        TemplateType ITemplate.TemplateType => templateType;
        public TemplateType TemplateType => ((ITemplate)this).TemplateType;
        
        // === Attachment Group
        public virtual string AttachGroupId => DefaultAttachmentGroupName;
        public virtual bool StartEnabled => true;

        public virtual IEnumerable<IAttachmentSpec> GetAttachments() {
            var allAttachments = this.AllAttachments();
            foreach (var attachment in allAttachments.value) {
                if (attachment is not IAttachmentGroup) {
                    yield return attachment;
                }
            }
            allAttachments.Release();
        }

        public virtual PooledList<IAttachmentGroup> GetAttachmentGroups() {
            PooledList<IAttachmentGroup>.Get(out var results);
            GetComponentsInChildren<IAttachmentGroup>(true, results);
            return results;
        }
    }
}