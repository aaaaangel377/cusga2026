ImageColliderFile 使用说明
========================

功能：
1. 游戏启动时，将物体的 SpriteRenderer 导出为 PNG 到 level/{levelIndex}/{fileName}.png
2. 定时检查 PNG 文件是否被修改，如果修改则更新 SpriteRenderer 和碰撞箱
3. 根据图片的深色区域生成多个独立的碰撞路径（破碎区域不连接）
4. 当图片分辨率改变时，自动调整物体大小

使用方法：
1. 将 ImageColliderFile.cs 脚本挂载到带有 SpriteRenderer 的 GameObject 上
2. 在 Inspector 中设置：
   - File Name: 自定义文件名（留空则使用 GameObject 名称）
   - Check Interval: 文件检查间隔（默认 0.5 秒）
   - Threshold: 二值化阈值（0-255，默认 128，低于此值视为深色区域）
   - Curve Accuracy: 多边形逼近精度（越大越简化，默认 2）
   - Min Area: 最小轮廓面积（过滤小噪点，默认 100）
3. 确保场景中有 LevelFileManager
4. 运行游戏，PNG 文件会自动生成到 level/{levelIndex}/ 文件夹
5. 用外部图像编辑器修改 PNG 文件，物体会自动更新

注意事项：
- 需要 OpenCvSharp 库支持
- 深色区域（低于 threshold）会生成碰撞箱
- 每个独立的深色区域会生成独立的碰撞路径
- 图片分辨率改变时，物体会自动缩放

调试：
- 勾选 Show Debug Info 可以在 Inspector 中查看生成的碰撞路径数量和总点数
