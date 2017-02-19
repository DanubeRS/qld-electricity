import * as GlobalStore from "../store/reducers/global";
import {State} from '../store/index'
import * as React from "react";
import {connect, Dispatch} from "react-redux";
/**
 * Created by daniel on 18/2/17.
 */

const mapStateToProps = (state: State) => (
    {test : state.global.test} as StateProps
);
const mapDispatchToProps = (dispatch: Dispatch<any>) => {
    return {
        update: () => {
            dispatch(GlobalStore.updateDate());
        }
    } as DispatchProps
};

interface DispatchProps {
    update: () => void;
}
interface StateProps {
    test: Date;
}

class DateRefresher extends React.Component<DispatchProps & StateProps, any> {
    render(): JSX.Element | any {
        return <div>{this.props.test.toTimeString()}
            <button type="button" onClick={e => this.clicked(e)}>Test</button>
        </div>
    }

    clicked(event: any) {
        this.props.update();
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(DateRefresher);

