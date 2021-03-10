using System;
using Volo.Abp.Application.Dtos;

namespace ProductManagement
{
    /// <summary>
    /// 模型
    /// </summary>
    public class ProductDto : AuditedEntityDto<Guid>
    {
        /// <summary>
        /// Code值
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 图片地址
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        public float Price { get; set; }

        /// <summary>
        /// 存量
        /// </summary>
        public int StockCount { get; set; }
    }
}