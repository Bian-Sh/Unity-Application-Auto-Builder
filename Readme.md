# Application Auto Builder

# 作用：

一次配置，一键打包，输出多个应用，看图：
one config, one click, output multiple applications, see the picture below:

![](doc/interface.png)

支持在打包前通过 Task 修改场景内数据:
Support modifying scene data through Task before packaging:

| Function Execute Task                                                                      | Run Process Task                                                                                                                                                       |
| ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ![](doc/FuncExecuteTask.png)                                                               | ![](doc/RunProcessTask.png)                                                                                                                                            |
| 通过这个 task ，你可以写逻辑在打包前后对 scene 内的任意数据进行修改                                                   | 通过这个 Task，你可以在构建前、后运行一个应用程序，可以设置是否卡主线程，方便控制打包流程                                                                                                                        |
| With this task, you can write logic to modify any data in the scene before and after build | With this Task, you can run an application before and after the build, and you can set whether to block the main thread to facilitate the control of the build process |

# 动图演示：

![](doc/autobuilder.gif)

# 扩展阅读：

[[Unity 3D] 多场景任意组合的一键出多包的打包工具](https://www.jianshu.com/p/4ad5be33b60b?v=1667139567703) 
