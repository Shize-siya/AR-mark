# 知识条目：基础数据结构

**条目ID：** ENTRY-003  
**分类：** 数据结构  
**关键词：** 数据模型, JSON, 字段定义, 枚举值, Issue  
**重要程度：** 高

---

## 数据结构约束
三端字段名称应保持一致。

## 完整数据结构
```json
{
  "id": "由后端生成",
  "title": "入口墙面破损",
  "description": "左侧墙体存在裂缝",
  "priority": "high",
  "status": "open",
  "position": {
    "x": 0.42,
    "y": 0.03,
    "z": 1.26
  },
  "createdAt": "由后端生成",
  "updatedAt": "由后端生成"
}
```

## 字段说明

| 字段 | 类型 | 必填 | 说明 | 枚举值 |
|------|------|------|------|--------|
| id | string | 自动生成 | 问题唯一标识符，后端生成 | UUID |
| title | string | 是 | 问题标题，不能为空 | - |
| description | string | 否 | 问题详细描述 | - |
| priority | string | 是 | 优先级 | low / medium / high |
| status | string | 自动生成 | 处理状态，默认 open | open / in_progress / resolved |
| position.x | float | 是 | AR 标记世界坐标 X | - |
| position.y | float | 是 | AR 标记世界坐标 Y | - |
| position.z | float | 是 | AR 标记世界坐标 Z | - |
| createdAt | string | 自动生成 | 创建时间，ISO 8601 格式 | - |
| updatedAt | string | 自动生成 | 更新时间，ISO 8601 格式 | - |

## 重要说明
不要求应用重启后，把历史标记恢复到完全相同的真实物理位置。position 字段仅作为数据记录保存。

## 来源
飞书文档《Unity-AR研发实操题-候选人任务书-简略版》第 4 节
