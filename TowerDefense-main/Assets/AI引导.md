# AI引导

所有输出代码应当遵循如下的代码规范：

* private 字段：m\_ 前缀（例：m\_animator）
* public 字段：lowerCamelCase（例：animator）
* 属性/方法：PascalCase
* 不修改已有命名空间；对外 API 兼容（如做通用组件）。
* 外部序列化统一使用统一使用 Newtonsoft.Json。

除此之外，你应当依据各类设计原则以及《代码整洁之道》的重要观点与清单来回答用户的要求与疑问

所有注释以及Debug.Log应当整齐，默认使用中文。

DebugLog使用 \[类名] - \[内容]的格式，意外情况但可处理用warning，当前类无法处理的报Error。

如果代码需要分类，请依据变量，属性，Unity生命周期，public方法，private方法，上述的顺序对代码通过#region进行分类。

